using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class LevelParams
{
    public int difficultyLevel = 5;
    [Range(0f, 1f)] public float pitChance = 0.3f;      // 地面间隔生成概率
    [Range(0f, 1f)] public float platformChance = 0.2f; // 平台生成概率
    [Range(0f, 1f)] public float spikeChance = 0.05f;   // 障碍物生成概率
    public float spikeStayTime = 1f;                    // 障碍物停留时间
    [Range(0f, 1f)] public float slimeChance = 0.05f;   // 史莱姆生成概率
    public float slimeSpeed = 1f;                       // 史莱姆速度
    public int turretNums = 1;  // 炮塔生成概率
    public float turretDetectRange = 10f;               // 炮塔射击范围
    public float turretAimTime = 2f;                    // 炮塔准备时间

    // 这里可以扩展其他变量
}

