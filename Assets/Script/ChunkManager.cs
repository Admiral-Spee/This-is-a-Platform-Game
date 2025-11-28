using System.Collections.Generic;
using UnityEngine;
using System.Text;

public class ChunkManager : MonoBehaviour
{
    public bool testMode = false;
    public GameObject chunkPrefab;
    public int chunkWidth = 10;
    public int chunkHeight = 10;
    public int initialChunkCount = 3;      // 初始生成chunk数量，也作为缓冲区数量
    public int maxActiveChunks = 3;        // 场景保留chunk数量
    public float chunkOffset = 0;          // y轴偏移
    public Transform player;
    public float preGenerateDistance = 5f; // 提前多少距离生成下一个chunk

    public EmotionAggregator emotionAggregator;

    public DifficultyUIController uiController; // Inspector拖入

    private List<int> chunkDifficultyHistory = new List<int>();
    private List<int> chunkEmotionHistory = new List<int>();

    [Header("全局关卡参数")]
    public DifficultyConfig difficultyConfig;
    [Range(1, 10)] public int currentDifficultyLevel = 5; // Inspector/代码设置，1~10
    private List<ChunkGenerator2D> chunks = new List<ChunkGenerator2D>();
    private bool latestChunkEntered = false;

    void Start()
    {
        int randomBaseY = chunkPrefab.GetComponent<ChunkGenerator2D>().startFloorY;
        // 生成首批chunk
        for (int i = 0; i < initialChunkCount; i++)
        {
            ChunkGenerator2D chunk = CreateChunk(randomBaseY);
            chunks.Add(chunk);
        }
        if (uiController) uiController.UpdateDisplay(difficultyConfig.levels[currentDifficultyLevel - 1], difficultyConfig.levels[currentDifficultyLevel - 1]);
    }

    void Update()
    {
        // 检查是否需要提前生成新chunk
        // 只要玩家快接近倒数第N个chunk结尾就生成新chunk
        // N = initialChunkCount
        int triggerChunkIndex = chunks.Count - initialChunkCount;
        if (triggerChunkIndex >= 0)
        {
            ChunkGenerator2D triggerChunk = chunks[triggerChunkIndex];
            float triggerX = triggerChunk.transform.position.x + chunkWidth - preGenerateDistance;
            if (player.position.x > triggerX)
            {
                if (testMode)
                {
                    string dominant = emotionAggregator.GetMostFrequentEmotion();
                    if (dominant == "Angry")
                    {
                        DecreaseDifficulty();
                        DecreaseDifficulty();
                        chunkEmotionHistory.Add(0);
                    }
                    else if (dominant == "Happy")
                    {
                        //IncreaseDifficulty();
                        chunkEmotionHistory.Add(1);
                    }
                    else if (dominant == "Disgust")
                    {
                        IncreaseDifficulty();
                        IncreaseDifficulty();
                        chunkEmotionHistory.Add(2);
                    }
                    else if (dominant == "Other")
                    {
                        IncreaseDifficulty();
                        chunkEmotionHistory.Add(3);
                    }
                    else if (dominant == "Offline")
                    {
                        IncreaseDifficulty();
                        chunkEmotionHistory.Add(9);
                    }
                }
                else
                {
                    IncreaseDifficulty();
                }
                

                // 生成新chunk
                int randomBaseY = chunkPrefab.GetComponent<ChunkGenerator2D>().startFloorY;
                ChunkGenerator2D newChunk = CreateChunk(randomBaseY);
                chunks.Add(newChunk);
                latestChunkEntered = false;

                // 回收最早的chunk，保证数量不超过maxActiveChunks
                if (chunks.Count > maxActiveChunks)
                {
                    Destroy(chunks[0].gameObject);
                    chunks.RemoveAt(0);
                }
            }
            if (player.position.x > triggerChunk.transform.position.x && !latestChunkEntered && testMode)
            {
                emotionAggregator.ResetRecords();
                Debug.Log($"[ChunkManager] 清空情绪统计队列");
                latestChunkEntered = true;
            }
        }

        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            DecreaseDifficulty();
            
        }
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            IncreaseDifficulty();
            
        }
    }

    // 生成一个chunk，index为序号，startFloorY为起始高度
    ChunkGenerator2D CreateChunk(int randomBaseY)
    {
        // 关键点：计算新chunk的世界坐标位置
        Vector3 chunkPos;
        if (chunks.Count == 0)
        {
            chunkPos = new Vector3(0, chunkOffset, 0);
        }
        else
        {
            // 让新chunk紧跟在最后一个chunk右侧
            ChunkGenerator2D lastChunk = chunks[chunks.Count - 1];
            chunkPos = lastChunk.transform.position + new Vector3(chunkWidth, 0, 0);
        }

        GameObject go = Instantiate(chunkPrefab, chunkPos, Quaternion.identity, this.transform);
        ChunkGenerator2D chunk = go.GetComponent<ChunkGenerator2D>();

        chunk.chunkWidth = chunkWidth;
        chunk.chunkHeight = chunkHeight;

        LevelParams levelParams = difficultyConfig.levels[currentDifficultyLevel - 1]; // 下标0开始

        // 动态参数注入
        chunk.SetParams(levelParams);

        chunk.GenerateFloorChunk(randomBaseY);
        chunk.GeneratePlatforms();
        chunk.PaintGridToTilemap();
        chunk.GenerateObstacles();
        chunk.GenerateSlime();
        chunk.GenerateTurret();

        // 记录本chunk的难度
        chunkDifficultyHistory.Add(currentDifficultyLevel - 1);

        return chunk;
    }

    // 设置等级方法（自动修正合法区间）
    public void SetDifficultyLevel(int level)
    {
        currentDifficultyLevel = Mathf.Clamp(level, 1, difficultyConfig.levels.Length);
        Debug.Log($"[ChunkManager] 难度已切换到等级 {currentDifficultyLevel}");
        // 可刷新UI
    }

    LevelParams CopyParams(LevelParams param)
    {
        return new LevelParams
        {
            difficultyLevel = param.difficultyLevel,

            pitChance = param.pitChance,
            platformChance = param.platformChance,

            spikeChance = param.spikeChance,
            spikeStayTime = param.spikeStayTime,

            slimeChance = param.slimeChance,
            slimeSpeed = param.slimeSpeed,

            turretNums = param.turretNums,
            turretDetectRange = param.turretDetectRange,
            turretAimTime = param.turretAimTime
        };
    }

    // 难度提升
    public void IncreaseDifficulty()
    {
        var oldParams = CopyParams(difficultyConfig.levels[currentDifficultyLevel - 1]);

        SetDifficultyLevel(currentDifficultyLevel + 1);

        //currentParams.difficultyLevel = Mathf.Clamp(currentParams.difficultyLevel + 1, 0, 10);

        //currentParams.pitChance = Mathf.Clamp(currentParams.pitChance + 0.06f, 0f, 0.6f);
        //currentParams.platformChance = Mathf.Clamp(currentParams.platformChance - 0.02f, 0.1f, 0.3f);

        //currentParams.spikeChance = Mathf.Clamp(currentParams.spikeChance + 0.02f, 0.05f, 0.25f);
        //currentParams.spikeStayTime = Mathf.Clamp(currentParams.spikeStayTime - 0.1f, 0.5f, 1.5f);

        //currentParams.slimeChance = Mathf.Clamp(currentParams.slimeChance + 0.003f, 0.015f, 0.045f);
        //currentParams.slimeSpeed = Mathf.Clamp(currentParams.slimeSpeed + 0.1f, 0.5f, 1.5f);

        //currentParams.turretChance = Mathf.Clamp(currentParams.turretChance + 0.002f, 0.01f, 0.03f);
        //currentParams.turretDetectRange = Mathf.Clamp(currentParams.turretDetectRange + 1f, 5f, 15f);
        //currentParams.turretAimTime = Mathf.Clamp(currentParams.turretAimTime - 0.2f, 1f, 3f);

        if (uiController) uiController.UpdateDisplay(oldParams, difficultyConfig.levels[currentDifficultyLevel - 1]);
    }

    public void KeepDifficulty()
    {
        // 不变
    }

    public void DecreaseDifficulty()
    {
        var oldParams = CopyParams(difficultyConfig.levels[currentDifficultyLevel - 1]);

        SetDifficultyLevel(currentDifficultyLevel - 1);

        //currentParams.difficultyLevel = Mathf.Clamp(currentParams.difficultyLevel - 1, 0, 10);

        //currentParams.pitChance = Mathf.Clamp(currentParams.pitChance - 0.06f, 0f, 0.6f);
        //currentParams.platformChance = Mathf.Clamp(currentParams.platformChance + 0.02f, 0.1f, 0.3f);

        //currentParams.spikeChance = Mathf.Clamp(currentParams.spikeChance - 0.02f, 0.05f, 0.25f);
        //currentParams.spikeStayTime = Mathf.Clamp(currentParams.spikeStayTime + 0.1f, 0.5f, 1.5f);

        //currentParams.slimeChance = Mathf.Clamp(currentParams.slimeChance - 0.003f, 0.015f, 0.045f);
        //currentParams.slimeSpeed = Mathf.Clamp(currentParams.slimeSpeed - 0.1f, 0.5f, 1.5f);

        //currentParams.turretChance = Mathf.Clamp(currentParams.turretChance - 0.002f, 0.01f, 0.03f);
        //currentParams.turretDetectRange = Mathf.Clamp(currentParams.turretDetectRange - 1f, 5f, 15f);
        //currentParams.turretAimTime = Mathf.Clamp(currentParams.turretAimTime + 0.2f, 1f, 3f);

        if (uiController) uiController.UpdateDisplay(oldParams, difficultyConfig.levels[currentDifficultyLevel - 1]);
    }

    // 游戏结束时调用，返回难度等级串
    public string OutputChunkDifficultyHistory()
    {
        StringBuilder sb = new StringBuilder();
        foreach (int level in chunkDifficultyHistory)
        {
            sb.Append(level);
        }
        string result = sb.ToString();
        Debug.Log($"本局难度序列：{result}");
        return result;
    }
    public string OutputChunkEmotionHistory()
    {
        StringBuilder sb = new StringBuilder();
        foreach (int level in chunkEmotionHistory)
        {
            sb.Append(level);
        }
        string result = sb.ToString();
        Debug.Log($"本局情绪序列：{result}");
        return result;
    }
}
