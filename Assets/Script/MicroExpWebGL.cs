using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.UnityUtils;

using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;

// 为避免二义性
using OpenCVRect = OpenCVForUnity.CoreModule.Rect;
using UnityRect = UnityEngine.Rect;

public class MicroExpWebGL : MonoBehaviour
{
    [Header("UI References")]
    public RawImage[] displays;
    public AspectRatioFitter[] displayFitters;
    public TMP_Text[] resultTexts;

    [Header("Camera Request")]
    public string preferredDeviceContains = "";
    public int[] fallbackWidths = { 640, 1280, 1920 };
    public int[] fallbackHeights = { 480, 720, 1080 };
    public int[] fallbackFps = { 30, 24, 15 };

    [Header("Camera Watchdog")]
    public float cameraHeartbeatTimeout = 3f;
    public float cameraRestartCooldown = 5f;
    public int maxRestartAttempts = 3;

    [Header("Model & Aggregator")]
    public TextAsset onnxModelAsset;
    public string[] labels = { "Angry", "Happy", "Disgust" };
    public EmotionAggregator emotionAggregator;
    public string noResultText = "No Result";

    [Header("Preprocess")]
    public bool useHistEq = true;   // 直方图均衡开关（训练若没做过，可关掉对比）

    [Header("Temporal")]
    public int frameCount = 96;     // 模型时间窗 T

    [Header("UI Throttle")]
    public int uiFps = 30;

    [Header("Status UI (Camera)")]
    public TMP_Text[] cameraStatusTexts;
    public Image[] cameraStatusBadges;
    public Color camOnColor = new Color(0f, 1f, 0f, 1f);
    public Color camOffColor = new Color(1f, 0f, 0f, 1f);
    public string camOnText = "Camera: ON";
    public string camOffText = "Camera: OFF";

    [Header("Controls (Buttons)")]
    public Button[] stopCameraButtons;
    public Button[] returnMenuButtons;
    public string mainMenuSceneName = "MainMenu";

    [Header("Label / Output Remap")]
    public int[] classRemap = { 0, 1, 2 }; // 若怀疑 Happy/Angry 互换，试 {1,0,2}

    // 运行期
    WebCamTexture camTex;
    InferenceSession session;
    string inputName;

    Mat rgba;        // 摄像头 RGBA
    Mat gray;        // 灰度
    Mat small64;     // 64x64 灰度

    Texture2D sharedTex;
    float[] inputBuf;              // 环形缓冲，存原始像素 0..255
    int writeT = 0;
    int filled = 0;

    bool inferBusy = false;
    float uiTimer = 0f;

    public bool primed = false;
    string lastResultText = "";

    bool cameraRunning = false;
    bool modelEnabled = true;
    float lastFrameTs = 0f;
    float lastRestartTs = -999f;
    int restartAttempts = 0;

    [SerializeField] int maxInferenceErrors = 3;
    int inferenceErrorCount = 0;

    void Start()
    {
        Diagnostics.I("APP", "App Start");
        Diagnostics.I("ENV", $"Screen {Screen.width}x{Screen.height} | {Application.platform} | Unity {Application.unityVersion}");

        if (stopCameraButtons != null)
            foreach (var b in stopCameraButtons) if (b) b.onClick.AddListener(StopCameraAndModel);
        if (returnMenuButtons != null)
            foreach (var b in returnMenuButtons) if (b) b.onClick.AddListener(ReturnToMainMenu);

        if (displayFitters != null)
            foreach (var f in displayFitters) if (f) f.aspectMode = AspectRatioFitter.AspectMode.FitInParent;

        // 模型资源判空
        if (onnxModelAsset == null || onnxModelAsset.bytes == null || onnxModelAsset.bytes.Length == 0)
        {
            MarkCameraError("ORT_MODEL_MISSING", "ONNX model TextAsset is null or empty");
            modelEnabled = false;
        }
        else
        {
            // ONNX Session
            try
            {
                var so = new SessionOptions
                {
                    InterOpNumThreads = System.Environment.ProcessorCount,
                    IntraOpNumThreads = System.Environment.ProcessorCount,
                    GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_EXTENDED,
                    LogSeverityLevel = OrtLoggingLevel.ORT_LOGGING_LEVEL_VERBOSE
                };
                so.AppendExecutionProvider_CPU();

                session = new InferenceSession(onnxModelAsset.bytes, so);
                inputName = session.InputMetadata.Keys.First();

                var dims = session.InputMetadata[inputName].Dimensions;
                Diagnostics.I("ORT", $"Session created. Input dims = [{string.Join(",", dims)}]");

                // 若模型给出固定的 T，自动与 frameCount 对齐
                if (dims.Length >= 5 && dims[4] > 0 && frameCount != (int)dims[4])
                {
                    Diagnostics.W("ORT", $"frameCount {frameCount} != model T {dims[4]}, aligning.");
                    frameCount = (int)dims[4];
                }
            }
            catch (Exception ex)
            {
                MarkCameraError("ORT_INIT_FAILED", $"ONNX init failed: {ex.Message}");
                modelEnabled = false;
            }
        }

        // 缓冲分配（根据可能对齐后的 frameCount）
        inputBuf = new float[1 * 1 * 64 * 64 * frameCount];

        // 聚合器
        if (!emotionAggregator) emotionAggregator = FindObjectOfType<EmotionAggregator>();

        lastResultText = $"Collecting (0/{frameCount})";
        BroadcastText(lastResultText);
        UpdateCameraStatusUI(false);

        StartCoroutine(InitCameraRobust());
    }

    // ===== 相机初始化（权限 + 设备选择 + 分辨率/FPS 回退） =====
    System.Collections.IEnumerator InitCameraRobust()
    {
        Diagnostics.I("CAM", "InitCameraRobust begin");

        if (!Application.HasUserAuthorization(UserAuthorization.WebCam))
        {
            yield return Application.RequestUserAuthorization(UserAuthorization.WebCam);
            if (!Application.HasUserAuthorization(UserAuthorization.WebCam))
            {
                MarkCameraError("CAM_PERMISSION_DENIED", "Camera permission denied by user/OS");
                yield break;
            }
        }

        var devices = WebCamTexture.devices;
        if (devices == null || devices.Length == 0)
        {
            MarkCameraError("CAM_NO_DEVICE", "No camera device found");
            yield break;
        }

        int idx = 0;
        if (!string.IsNullOrEmpty(preferredDeviceContains))
        {
            for (int i = 0; i < devices.Length; i++)
                if (devices[i].name.ToLower().Contains(preferredDeviceContains.ToLower()))
                { idx = i; break; }
        }
        var dev = devices[idx];
        Diagnostics.I("CAM", $"Using device: {dev.name}");

        WebCamTexture tex = null;
        bool started = false;
        foreach (var w in fallbackWidths)
            foreach (var h in fallbackHeights)
                foreach (var f in fallbackFps)
                {
                    Diagnostics.I("CAM", $"Try {dev.name} {w}x{h}@{f}");
                    tex = new WebCamTexture(dev.name, w, h, f);
                    tex.Play();

                    float t = 0f;
                    while (t < 2f && tex.width <= 16) { t += Time.deltaTime; yield return null; }

                    if (tex.width > 16)
                    {
                        started = true;
                        camTex = tex;
                        cameraRunning = true;
                        modelEnabled = modelEnabled && (session != null); // 只有 session 成功才启用推理
                        lastFrameTs = Time.time;
                        restartAttempts = 0;

                        UpdateCameraStatusUI(true);
                        Diagnostics.I("CAM", $"Started {tex.width}x{tex.height}@{f}");
                        yield break;
                    }
                    else
                    {
                        tex.Stop();
                    }
                }

        if (!started)
        {
            MarkCameraError("CAM_START_FAILED", "Failed to start camera (all resolutions/FPS tried)");
            ShowBlackFrameOnDisplays();
        }
    }

    void Update()
    {
        bool camAlive = cameraRunning && camTex != null && camTex.isPlaying && (Time.time - lastFrameTs) <= cameraHeartbeatTimeout;
        UpdateCameraStatusUI(camAlive);

        if (!cameraRunning || camTex == null) return;
        if (camTex.width < 16) return;

        EnsureMats(camTex.width, camTex.height);

        // 1) Webcam → RGBA
        Utils.webCamTextureToMat(camTex, rgba);
        if (camTex.didUpdateThisFrame)
        {
            lastFrameTs = Time.time;
            Diagnostics.FeedCameraHeartbeat();
        }

        // 2) 灰度 + （可选）均衡
        Imgproc.cvtColor(rgba, gray, Imgproc.COLOR_RGBA2GRAY);
        if (useHistEq) Imgproc.equalizeHist(gray, gray);

        // 3) 画面中心最大正方形 ROI（始终使用它）
        int W = rgba.cols(), H = rgba.rows();
        int side = Mathf.Min(W, H);
        int x0 = (W - side) / 2;
        int y0 = (H - side) / 2;
        OpenCVRect centerRect = new OpenCVRect(x0, y0, side, side);

        // 4) 入队：中心 ROI 的灰度 → 64×64 → 写环形（存原始 0..255）
        using (var roiGray = new Mat(gray, centerRect))
        {
            Imgproc.resize(roiGray, small64, new Size(64, 64));
            Push64x64RawToRing(small64, writeT);
            writeT = (writeT + 1) % frameCount;
            filled = Mathf.Min(frameCount, filled + 1);

#if UNITY_EDITOR
            if (writeT % 24 == 0)
            {
                int slice = 64 * 64;
                int off = ((writeT - 1 + frameCount) % frameCount) * slice;
                float mn = float.PositiveInfinity, mx = float.NegativeInfinity;
                for (int i = 0; i < slice; i++) { float v = inputBuf[off + i]; if (v < mn) mn = v; if (v > mx) mx = v; }
                Diagnostics.I("BUF", $"t={((writeT - 1 + frameCount) % frameCount)} min={mn:F1} max={mx:F1}");
            }
#endif
        }

        // 5) 满窗且不在推理中 → 异步推理（展开环形 → 归一化 → 重排 T 维）
        if (modelEnabled && session != null && !inferBusy && filled == frameCount)
        {
            primed = true;
            inferBusy = true;

            Diagnostics.I("INF", "Run ORT");

            var slice = 64 * 64;
            var snap = MakeContiguousSnapshot(inputBuf, frameCount, slice, writeT);
            NormalizeLikeTraining(snap);

#if UNITY_EDITOR
            {
                double sum = 0, sum2 = 0; float mn = float.PositiveInfinity, mx = float.NegativeInfinity;
                for (int i = 0; i < snap.Length; i++) { var v = snap[i]; sum += v; sum2 += v * v; if (v < mn) mn = v; if (v > mx) mx = v; }
                double mean = sum / snap.Length; double std = Math.Sqrt(Math.Max(0, sum2 / snap.Length - mean * mean));
                Diagnostics.I("SNAP", $"min={mn:F3} max={mx:F3} mean={mean:F3} std={std:F3}");
            }
#endif

            Task.Run(() =>
            {
                using (Diagnostics.Measure("INF", "ORT.Run"))
                {
                    // 将时间主序拼接的 snap 重排成“最后一维 T 变化最快”的布局
                    var feed = ReorderTimeMajorToLastDimFastest(snap, 64, 64, frameCount);
                    var tensor = new DenseTensor<float>(feed, new[] { 1, 1, 64, 64, frameCount });

                    using var res = session.Run(new[] { NamedOnnxValue.CreateFromTensor(inputName, tensor) });
                    return res.First().AsTensor<float>().ToArray();
                }
            })
            .ContinueWith(task =>
            {
                inferBusy = false;
                if (task.Exception != null)
                {
                    inferenceErrorCount++;
                    Diagnostics.W("INF", $"Inference failed #{inferenceErrorCount}: {task.Exception.Flatten().InnerException?.Message}");
                    BroadcastText("Inference error");
                    if (inferenceErrorCount >= maxInferenceErrors)
                        MarkCameraError("ORT_INFERENCE_FAILED", $"Inference failed {inferenceErrorCount} times");
                    return;
                }
                inferenceErrorCount = 0;

                var outv = task.Result;

                // ① 类别通道重映射（Inspector 可配置）
                var outRemapped = ApplyClassRemap(outv, classRemap);

                // ② 聚合 & 显示
                emotionAggregator?.AddResult(outRemapped);

                var (best, prob) = Top1(outRemapped);
                lastResultText = $"{labels[best]} ({prob:F3})";
                Diagnostics.I("INF", $"Top1={labels[best]} {prob:F3}");

#if UNITY_EDITOR
                var idx = Enumerable.Range(0, outRemapped.Length).OrderByDescending(i => outRemapped[i]).Take(3).ToArray();
                Diagnostics.I("INF", $"Top3(remap): {labels[idx[0]]}:{outRemapped[idx[0]]:F3}, {labels[idx[1]]}:{outRemapped[idx[1]]:F3}, {labels[idx[2]]}:{outRemapped[idx[2]]:F3}");
#endif

                BroadcastText(lastResultText);
            }, TaskScheduler.FromCurrentSynchronizationContext());
        }
        else
        {
            if (!primed) BroadcastTextThrottled($"Collecting ({filled}/{frameCount})");
        }

        // 6) 显示：把“中心 ROI 的彩色画面”推给 RawImage，并在边缘画框（可视化）
        using (Mat colorCrop = new Mat(rgba, centerRect))
        {
            Imgproc.rectangle(
                colorCrop,
                new Point(0, 0),
                new Point(colorCrop.cols() - 1, colorCrop.rows() - 1),
                new Scalar(0, 255, 0, 255), 2
            );

            uiTimer += Time.deltaTime;
            if (uiTimer >= (1f / Mathf.Max(1, uiFps)))
            {
                uiTimer = 0f;

                if (sharedTex == null || sharedTex.width != colorCrop.cols() || sharedTex.height != colorCrop.rows())
                    sharedTex = new Texture2D(colorCrop.cols(), colorCrop.rows(), TextureFormat.RGBA32, false);

                Utils.matToTexture2D(colorCrop, sharedTex);
                BroadcastTexture(sharedTex);
            }
        }

#if UNITY_EDITOR
        if (writeT % 24 == 0 && filled > 0)
        {
            int slice = 64 * 64;
            int off = ((writeT - 1 + frameCount) % frameCount) * slice;
            float mn = float.PositiveInfinity, mx = float.NegativeInfinity;
            for (int i = 0; i < slice; i++) { float v = inputBuf[off + i]; if (v < mn) mn = v; if (v > mx) mx = v; }
            Diagnostics.I("BUF", $"t={((writeT - 1 + frameCount) % frameCount)} min={mn:F1} max={mx:F1}");
        }
#endif
    }

    void LateUpdate()
    {
        if (cameraRunning && camTex != null)
        {
            bool lost = (Time.time - lastFrameTs) > cameraHeartbeatTimeout;
            if (lost && (Time.time - lastRestartTs) > cameraRestartCooldown)
            {
                restartAttempts++;
                if (restartAttempts > maxRestartAttempts)
                {
                    MarkCameraError("CAM_HEARTBEAT_LOST", $"No frames for {cameraHeartbeatTimeout}s, {restartAttempts - 1} restarts attempted");
                    return;
                }
                Diagnostics.W("CAM", $"Heartbeat lost. Restarting... ({restartAttempts}/{maxRestartAttempts})");
                lastRestartTs = Time.time;
                StartCoroutine(RestartCameraOnly());
            }
        }
    }

    // ==== 归一化 / 展开 / 入队 ====
    void NormalizeLikeTraining(float[] a)
    {
        double sum = 0; float maxv = float.NegativeInfinity;
        for (int i = 0; i < a.Length; i++) { var v = a[i]; sum += v; if (v > maxv) maxv = v; }
        double mean = sum / a.Length;
        float denom = (maxv > 1e-7f) ? maxv : 1e-7f;
        for (int i = 0; i < a.Length; i++) a[i] = (float)((a[i] - mean) / denom);
    }

    float[] MakeContiguousSnapshot(float[] ring, int T, int slice, int writeIndex)
    {
        var snap = new float[ring.Length];
        for (int i = 0; i < T; i++)
        {
            int ringT = (writeIndex + i) % T; // 最老 = writeIndex
            Buffer.BlockCopy(ring, ringT * slice * sizeof(float),
                                   snap, i * slice * sizeof(float),
                                   slice * sizeof(float));
        }
        return snap;
    }

    // 将时间主序的展平数组 snap（按 t 片拼接，每片为 64*64）
    // 重排成 DenseTensor 期望的“最后一维 T 变化最快”的内存布局：index = ((y*W + x) * T + t)
    float[] ReorderTimeMajorToLastDimFastest(float[] snap, int H, int W, int T)
    {
        int slice = H * W;
        var dst = new float[snap.Length];
        int p = 0;
        for (int y = 0; y < H; y++)
        {
            for (int x = 0; x < W; x++)
            {
                int hw = y * W + x;
                for (int t = 0; t < T; t++)
                {
                    dst[p++] = snap[t * slice + hw];
                }
            }
        }
        return dst;
    }

    void Push64x64RawToRing(Mat m64, int tIndex)
    {
        int slice = 64 * 64;
        int off = tIndex * slice;
        int k = 0;
        for (int y = 0; y < 64; y++)
            for (int x = 0; x < 64; x++)
                inputBuf[off + k++] = (float)m64.get(y, x)[0]; // 原始 0..255
    }

    System.Collections.IEnumerator RestartCameraOnly()
    {
        if (camTex != null) { try { camTex.Stop(); } catch { } }
        yield return null;
        yield return InitCameraRobust();
    }

    // ===== UI & 控制 =====
    void UpdateCameraStatusUI(bool on)
    {
        if (cameraStatusTexts != null)
            foreach (var t in cameraStatusTexts) if (t) t.text = on ? camOnText : camOffText;

        if (cameraStatusBadges != null)
            foreach (var img in cameraStatusBadges) if (img) img.color = on ? camOnColor : camOffColor;
    }

    public void StopCameraAndModel()
    {
        Diagnostics.W("CAM", "Stop camera & model");
        modelEnabled = false;
        cameraRunning = false;

        if (camTex != null) { try { camTex.Stop(); } catch { } }
        camTex = null; // 下次进入场景会重新初始化

        inferBusy = false;
        filled = 0;
        writeT = 0;
        primed = false;

        UpdateCameraStatusUI(false);
        ShowBlackFrameOnDisplays();

        emotionAggregator?.SetOffline(true);

        lastResultText = noResultText;
        BroadcastText(lastResultText);

        if (stopCameraButtons != null)
            foreach (var b in stopCameraButtons) if (b) b.interactable = false;
    }

    public void ReturnToMainMenu()
    {
        StopCameraAndModel();
        Diagnostics.I("APP", $"LoadScene {mainMenuSceneName}");
        SceneManager.LoadScene(mainMenuSceneName);
    }

    // ===== 工具：Mat/纹理/广播 =====
    void EnsureMats(int cw, int ch)
    {
        if (rgba == null || rgba.cols() != cw || rgba.rows() != ch)
        {
            rgba = new Mat(ch, cw, CvType.CV_8UC4);
            gray = new Mat(ch, cw, CvType.CV_8UC1);
        }
        if (small64 == null) small64 = new Mat(64, 64, CvType.CV_8UC1);
    }

    void BroadcastText(string s)
    {
        if (resultTexts == null) return;
        foreach (var t in resultTexts) if (t) t.text = s;
    }
    void BroadcastTextThrottled(string s) => BroadcastText(s);

    void BroadcastTexture(Texture2D tex)
    {
        if (displays != null)
            foreach (var ri in displays) if (ri) ri.texture = tex;

        if (displayFitters != null)
        {
            float ar = tex.width / (float)tex.height;
            foreach (var f in displayFitters) if (f) f.aspectRatio = ar;
        }
    }

    void ShowBlackFrameOnDisplays()
    {
        if (displays != null)
            foreach (var ri in displays) if (ri) ri.texture = Texture2D.blackTexture;

        if (displayFitters != null)
        {
            foreach (var f in displayFitters)
            {
                if (!f) continue;
                var rt = f.GetComponent<RectTransform>();
                if (!rt) continue;
                float w = Mathf.Max(1f, rt.rect.width);
                float h = Mathf.Max(1f, rt.rect.height);
                f.aspectRatio = w / h;
            }
        }
    }

    void MarkCameraError(string code, string message)
    {
        Diagnostics.E(code, message);
        UpdateCameraStatusUI(false);
        ShowBlackFrameOnDisplays();
        lastResultText = $"{noResultText} ({code})";
        BroadcastText(lastResultText);
        modelEnabled = false;
        cameraRunning = false;
        emotionAggregator?.SetOffline(true);

        if (stopCameraButtons != null)
            foreach (var b in stopCameraButtons) if (b) b.interactable = false;
    }

    (int idx, float prob) Top1(float[] probs)
    {
        int best = 0; float p = probs[0];
        for (int i = 1; i < probs.Length; i++)
            if (probs[i] > p) { p = probs[i]; best = i; }
        return (best, p);
    }

    // 类别通道重映射（安全性检测）
    float[] ApplyClassRemap(float[] probs, int[] remap)
    {
        if (remap == null || remap.Length != probs.Length) return probs;
        for (int i = 0; i < remap.Length; i++)
            if (remap[i] < 0 || remap[i] >= probs.Length) return probs;
        var dst = new float[probs.Length];
        for (int i = 0; i < probs.Length; i++) dst[i] = probs[remap[i]];
        return dst;
    }

    void OnDestroy()
    {
        if (stopCameraButtons != null)
            foreach (var b in stopCameraButtons) if (b) b.onClick.RemoveListener(StopCameraAndModel);
        if (returnMenuButtons != null)
            foreach (var b in returnMenuButtons) if (b) b.onClick.RemoveListener(ReturnToMainMenu);

        try { camTex?.Stop(); } catch { }
        camTex = null;

        session?.Dispose(); session = null;

        rgba?.Dispose();
        gray?.Dispose();
        small64?.Dispose();
    }
}
