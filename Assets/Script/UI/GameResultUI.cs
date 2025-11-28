using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GameResultUI : MonoBehaviour
{
    public bool testMode = false;
    public static GameResultUI Instance; // 单例方便调用
    public GameObject panel;             // 结算面板主节点
    public TMP_Text timeText;
    public TMP_Text distanceText;
    public Button homeButton;
    public Button retryButton;
    public TMP_Text difficultyHistory;
    public TMP_Text emotionHistory;

    public ChunkManager chunkManager;

    void Awake()
    {
        Instance = this;
        panel.SetActive(false); // 默认隐藏
        homeButton.onClick.AddListener(ReturnHome);
        retryButton.onClick.AddListener(Retry);
    }

    public void ShowResult(float time, float distance)
    {
        panel.SetActive(true);
        timeText.text = $"{"Time / 用时", -15}{time.ToString("F2"), 12}{" S"}";
        distanceText.text = $"{"Distance / 距离", -15}{distance.ToString("F1"), 12}{" M"}";
        if (testMode)
        {
            difficultyHistory.text = $"{chunkManager.OutputChunkDifficultyHistory()}";
            emotionHistory.text = $"{chunkManager.OutputChunkEmotionHistory()}";
        }
        else
        {
            difficultyHistory.text = $"Null";
            emotionHistory.text = $"Null";
        }

    }

    void ReturnHome()
    {
        SceneManager.LoadScene("MainMenu"); // 你的主页场景名
    }

    void Retry()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
