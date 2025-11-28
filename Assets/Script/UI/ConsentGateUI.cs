using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class ConsentGateUI : MonoBehaviour
{
    [Header("Toggles")]
    [SerializeField] private Toggle isAdultToggle;      // 年满18岁
    [SerializeField] private Toggle agreeInfoToggle;    // 同意信息说明

    [Header("Buttons")]
    [SerializeField] private Button startButton;        // 开始游戏
    [SerializeField] private Button quitButton;         // 退出游戏
    [SerializeField] private Button openInfoButton;     // 打开测试者信息说明

    [Header("Start Behaviour")]
    [Tooltip("填写后，点击开始将直接加载该场景；留空则仅触发 OnStartApproved 事件。")]
    [SerializeField] private string nextSceneName = "";

    [Tooltip("在两个勾选均通过时触发，可在 Inspector 里额外挂自定义事件（如隐藏本页、播放过场等）。")]
    public UnityEngine.Events.UnityEvent OnStartApproved;

    [Header("Info Sheet")]
    [Tooltip("线上PDF或网页地址（优先使用）。例如：https://yourdomain.com/docs/TesterInformationSheet_UK_zh_v1.pdf")]
    [SerializeField] private string infoUrl = "";

    [Tooltip("StreamingAssets 内的同名PDF（离线兜底）。例如：TesterInformationSheet_UK_zh_v1.pdf")]
    [SerializeField] private string localInfoFileName = "TesterInformationSheet_UK_zh_v1.pdf";

    [Tooltip("当 infoUrl 为空或不可用时，尝试从 StreamingAssets 复制到 persistentDataPath 并打开本地文件。")]
    [SerializeField] private bool enableOfflineFallback = true;

    private void Awake()
    {
        // 保险：按钮与勾选框都已绑定时，再注册监听
        if (isAdultToggle) isAdultToggle.onValueChanged.AddListener(_ => RefreshStartButton());
        if (agreeInfoToggle) agreeInfoToggle.onValueChanged.AddListener(_ => RefreshStartButton());

        if (startButton) startButton.onClick.AddListener(HandleStartClicked);
        if (quitButton) quitButton.onClick.AddListener(HandleQuitClicked);
        if (openInfoButton) openInfoButton.onClick.AddListener(OpenInfoSheet);

        RefreshStartButton();
    }

    private void OnDestroy()
    {
        if (isAdultToggle) isAdultToggle.onValueChanged.RemoveAllListeners();
        if (agreeInfoToggle) agreeInfoToggle.onValueChanged.RemoveAllListeners();

        if (startButton) startButton.onClick.RemoveAllListeners();
        if (quitButton) quitButton.onClick.RemoveAllListeners();
        if (openInfoButton) openInfoButton.onClick.RemoveAllListeners();
    }

    private void RefreshStartButton()
    {
        bool ready = isAdultToggle != null && agreeInfoToggle != null
                     && isAdultToggle.isOn && agreeInfoToggle.isOn;
        if (startButton) startButton.interactable = ready;
    }

    private void HandleStartClicked()
    {
        // 双保险
        if (!(isAdultToggle && agreeInfoToggle && isAdultToggle.isOn && agreeInfoToggle.isOn))
        {
            Debug.LogWarning("ConsentGateUI: 条件未满足（18+ 或 同意未勾选）。");
            return;
        }

        OnStartApproved?.Invoke();

        if (!string.IsNullOrEmpty(nextSceneName))
        {
            // 可改为异步加载
            SceneManager.LoadScene(nextSceneName);
        }
    }

    private void HandleQuitClicked()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#elif UNITY_WEBGL
        // WebGL 上 Application.Quit() 无效；尽量给出提示。
        Debug.Log("ConsentGateUI: WebGL 不支持退出应用。请关闭浏览器标签页。");
#else
        Application.Quit();
#endif
    }

    public void OpenInfoSheet()
    {
        if (!string.IsNullOrEmpty(infoUrl))
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            // WebGL：让浏览器打开/下载
            Application.OpenURL(infoUrl);
#else
            Application.OpenURL(infoUrl);
#endif
            return;
        }

        if (enableOfflineFallback)
        {
            StartCoroutine(OpenLocalInfoCoroutine());
        }
        else
        {
            Debug.LogWarning("ConsentGateUI: 未设置 infoUrl，且未启用离线兜底。");
        }
    }

    private IEnumerator OpenLocalInfoCoroutine()
    {
        string src = Path.Combine(Application.streamingAssetsPath, localInfoFileName);
        string dst = Path.Combine(Application.persistentDataPath, localInfoFileName);

#if UNITY_ANDROID
        // Android: StreamingAssets 需用 UnityWebRequest 读取
        var req = UnityEngine.Networking.UnityWebRequest.Get(src);
        yield return req.SendWebRequest();
        if (req.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
        {
            File.WriteAllBytes(dst, req.downloadHandler.data);
        }
        else
        {
            Debug.LogError($"ConsentGateUI: 读取 StreamingAssets 失败: {req.error}");
            yield break;
        }
#else
        try
        {
            if (!File.Exists(dst)) File.Copy(src, dst, true);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"ConsentGateUI: 复制 PDF 失败：{e.Message}");
            yield break;
        }
#endif
        Application.OpenURL("file://" + dst);
    }
}
