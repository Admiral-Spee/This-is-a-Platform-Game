using UnityEngine;
using UnityEngine.UI;

public class StartOverlayUIController : MonoBehaviour
{
    public GameObject overlayRoot;

    public GameObject chunks;
    public GameObject player;

    public MicroExpWebGL microExpWebGL;

    public Button startButton;     // 点击后：隐藏UI + 激活目标


    void Awake()
    {
        // 初始化 UI 显示
        if (overlayRoot != null) overlayRoot.SetActive(true);

        // 初始化 目标状态
        if (chunks != null)
            chunks.SetActive(false);
        if (player != null)
            player.SetActive(false);

        // 绑定按钮（可选）
        if (startButton != null) startButton.onClick.AddListener(OnClickStart);
    }

    void Update()
    {

        startButton.interactable = microExpWebGL.primed;

    }

    // “开始”按钮：隐藏UI并激活目标
    public void OnClickStart()
    {
        ResumeTarget();
        if (overlayRoot != null) overlayRoot.SetActive(false);
    }

    // 恢复目标
    public void ResumeTarget()
    {
        if (chunks != null) 
            chunks.SetActive(true);
        if (player != null)
            player.SetActive(true);
    }

}
