using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// 提供 ResetRecords() 与 GetMostFrequentEmotion() 两个接口给其他脚本调用。
public class EmotionAggregator : MonoBehaviour
{
    [Header("情绪标签（按模型输出顺序填写）")]
    public string[] labels = { "Angry", "Happy", "Disgust" };

    [Header("置信度阈值（低于该值统计为 Other）")]
    [Range(0f, 1f)]
    public float threshold = 0.5f;

    [Header("离线时覆盖输出")]
    public bool useOverrideWhenOffline = true;     // 勾选后，离线用 offlineLabel 覆盖
    public string offlineLabel = "Offline";        // 相机/模型停止时返回的“单独新标签”

    // 内部统计字典：记录每个标签出现次数，包括 "Other"
    private Dictionary<string, int> counts;

    private bool isOffline = false;                // 外部设置

    void Awake()
    {
        InitCounts();
    }

    // 初始化或清空统计字典
    private void InitCounts()
    {
        counts = new Dictionary<string, int>(labels.Length + 1);
        foreach (var lbl in labels)
            counts[lbl] = 0;
        counts["Other"] = 0;
    }


    // 对外接口：清空所有记录，开始新一轮统计
    public void ResetRecords()
    {
        InitCounts();
    }

    // 对外接口：向聚合器添加一次模型输出概率向量
    public void AddResult(float[] output)
    {
        if (isOffline) return;                     // 离线时直接忽略输入

        if (output == null || output.Length != labels.Length)
            throw new ArgumentException($"Output length must be {labels.Length}");

        // 找最大概率
        int idxMax = 0;
        float maxProb = output[0];
        for (int i = 1; i < output.Length; i++)
        {
            if (output[i] > maxProb)
            {
                maxProb = output[i];
                idxMax = i;
            }
        }

        // 根据阈值决定计入哪类
        string key = maxProb >= threshold
            ? labels[idxMax]
            : "Other";

        counts[key]++;
    }

    // 对外接口：获取当前出现次数最多的情绪标签
    // 统计中次数最多的标签（包括 "Other"）
    public string GetMostFrequentEmotion()
    {
        if (useOverrideWhenOffline && isOffline)   // ★ 离线覆盖输出
            return offlineLabel;

        // 按出现次数降序，返回第一个 key
        return counts
            .OrderByDescending(kv => kv.Value)
            .First().Key;
    }

    public void SetOffline(bool offline) => isOffline = offline;

    // 获取所有情绪及其出现次数的副本
    public Dictionary<string, int> GetAllCounts()
    {
        // 返回一个新的字典副本，避免外部修改内部状态
        return new Dictionary<string, int>(counts);
    }
}
