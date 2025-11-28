using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using TMPro;
using SDiagnostics = System.Diagnostics; // 仅用于 Stopwatch 别名

public class Diagnostics : MonoBehaviour
{
    [Header("File Logging")]
    public bool enableFileLogging = true;
    public string folderName = "logs";          // 相对 persistentDataPath
    public string filePrefix = "microexp";
    public int maxFileSizeKB = 1024 * 5;        // 5 MB
    public int maxFiles = 5;                    // 最多保留 N 个滚动文件

    [Header("Overlay (optional)")]
    public bool enableOverlay = true;
    public TMP_Text overlayText;                // 拖一个 TMP Text
    public int overlayMaxLines = 20;
    public bool showFps = true;

    [Header("Heartbeat")]
    public float cameraHeartbeatSeconds = 1.0f; // 超过 N 秒无帧，判定掉线

    static Diagnostics _instance;
    static readonly object _lock = new object();
    static Queue<string> recent = new Queue<string>();
    static string logDir, logFile;
    static StreamWriter writer;
    static float frameCounter, timeCounter, lastFps;
    static float lastCamFrameTime;             // 由外部喂入
    static bool inited;
    static bool overlayDirty;

    void Awake()
    {
        if (_instance != null && _instance != this) { Destroy(gameObject); return; }
        _instance = this;
        DontDestroyOnLoad(gameObject);
        InitLogging();
    }

    void OnEnable()
    {
        Application.logMessageReceivedThreaded += OnUnityLog;
    }

    void OnDisable()
    {
        Application.logMessageReceivedThreaded -= OnUnityLog;
        try { writer?.Flush(); writer?.Dispose(); } catch { }
        writer = null;
        inited = false;
    }

    void Update()
    {
        // FPS 统计
        if (showFps)
        {
            frameCounter++;
            timeCounter += Time.unscaledDeltaTime;  // 注意：这里的 Time = UnityEngine.Time
            if (timeCounter >= 0.5f)
            {
                lastFps = frameCounter / timeCounter;
                frameCounter = 0f;
                timeCounter = 0f;
                overlayDirty = true;
            }
        }

        // Overlay 刷新
        if (enableOverlay && overlayText)
        {
            if (overlayDirty)
            {
                overlayText.text = BuildOverlay();
                overlayDirty = false;
            }
        }
    }

    void InitLogging()
    {
        if (inited) return;

        logDir = Path.Combine(Application.persistentDataPath, folderName);
        Directory.CreateDirectory(logDir);
        RollFilesIfNeeded(forceNew: true);
        inited = true;

        LogInternal("SYS", "Diagnostics init. Path=" + logDir, LogType.Log);
        LogInternal("SYS", $"Unity {Application.unityVersion} | {SystemInfo.operatingSystem} | {SystemInfo.processorType}", LogType.Log);
    }

    static void RollFilesIfNeeded(bool forceNew = false)
    {
        try
        {
            writer?.Flush(); writer?.Dispose(); writer = null;

            // 轮转历史文件
            var di = new DirectoryInfo(logDir);
            var files = di.GetFiles($"{_instance.filePrefix}_*.log");
            Array.Sort(files, (a, b) => a.LastWriteTimeUtc.CompareTo(b.LastWriteTimeUtc));
            while (files.Length >= _instance.maxFiles)
            {
                files[0].Delete();
                files = di.GetFiles($"{_instance.filePrefix}_*.log");
            }

            // 新文件 or 追加
            if (forceNew || files.Length == 0 ||
                (files[^1].Length / 1024) > _instance.maxFileSizeKB)
            {
                var ts = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                logFile = Path.Combine(logDir, $"{_instance.filePrefix}_{ts}.log");
            }
            else
            {
                logFile = files[^1].FullName;
            }

            writer = new StreamWriter(new FileStream(logFile, FileMode.Append, FileAccess.Write, FileShare.ReadWrite), Encoding.UTF8)
            { AutoFlush = true };
        }
        catch (Exception ex)
        {
            Debug.LogWarning("Diagnostics roll failed: " + ex.Message);
        }
    }

    static string Now() => DateTime.Now.ToString("HH:mm:ss.fff");

    static void OnUnityLog(string condition, string stackTrace, LogType type)
    {
        string line = $"{Now()} [{type}] {condition}";
        lock (_lock)
        {
            if (_instance != null && _instance.enableFileLogging)
            {
                try
                {
                    if (writer == null) RollFilesIfNeeded();
                    writer?.WriteLine(line);
                    if (type == LogType.Exception || type == LogType.Error)
                        writer?.WriteLine(stackTrace);
                }
                catch { }
            }
            EnqueueOverlay(line);
        }
        overlayDirty = true;
    }

    static void EnqueueOverlay(string line)
    {
        if (_instance == null || !_instance.enableOverlay) return;
        recent.Enqueue(line);
        while (recent.Count > _instance.overlayMaxLines)
            recent.Dequeue();
    }

    static string BuildOverlay()
    {
        var sb = new StringBuilder();
        if (_instance.showFps)
        {
            sb.Append($"FPS: {lastFps:F1}");
            bool camAlive = (Time.time - lastCamFrameTime) <= _instance.cameraHeartbeatSeconds; // 使用 UnityEngine.Time
            sb.Append(camAlive ? " | Camera: ON" : " | Camera: OFF");
            sb.AppendLine();
        }
        foreach (var s in recent) sb.AppendLine(s);
        return sb.ToString();
    }

    // ====== 对外 API ======

    public static void I(string tag, string msg) => LogInternal(tag, msg, LogType.Log);
    public static void W(string tag, string msg) => LogInternal(tag, msg, LogType.Warning);
    public static void E(string tag, string msg, Exception ex = null)
    {
        if (ex != null) msg += " :: " + ex.Message;
        LogInternal(tag, msg, LogType.Error);
    }

    static void LogInternal(string tag, string msg, LogType type)
    {
        string line = $"{Now()} [{tag}] {msg}";
        lock (_lock)
        {
            if (_instance != null && _instance.enableFileLogging)
            {
                try
                {
                    if (writer == null) RollFilesIfNeeded();
                    writer?.WriteLine(line);
                }
                catch { }
            }
            EnqueueOverlay(line);
        }
        overlayDirty = true;

        // 同时将摘要发到控制台
        switch (type)
        {
            case LogType.Error: Debug.LogError(line); break;
            case LogType.Warning: Debug.LogWarning(line); break;
            default: Debug.Log(line); break;
        }
    }

    // 性能计时：using (Diagnostics.Measure("INF","ORT.Run")) { ... }
    public static IDisposable Measure(string tag, string name) => new TimerScope(tag, name);

    class TimerScope : IDisposable
    {
        string tag, name;
        SDiagnostics.Stopwatch sw;
        public TimerScope(string tag, string name)
        {
            this.tag = tag; this.name = name;
            sw = SDiagnostics.Stopwatch.StartNew();
        }
        public void Dispose()
        {
            sw.Stop();
            I(tag, $"{name} = {sw.ElapsedMilliseconds} ms");
        }
    }

    // 供摄像头帧到达时喂入心跳（Update 里 didUpdateThisFrame 为 true 时调用）
    public static void FeedCameraHeartbeat()
    {
        lastCamFrameTime = Time.time; // UnityEngine.Time
        overlayDirty = true;
    }

    // 打开日志文件夹（可绑按钮）
    public static void OpenLogFolder()
    {
        if (string.IsNullOrEmpty(logDir)) return;
        Application.OpenURL("file:///" + logDir.Replace("\\", "/"));
    }
}
