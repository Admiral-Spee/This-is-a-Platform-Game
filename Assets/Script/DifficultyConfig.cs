using UnityEngine;

[CreateAssetMenu(fileName = "DifficultyConfig", menuName = "Game/DifficultyConfig")]
public class DifficultyConfig : ScriptableObject
{
    public LevelParams[] levels = new LevelParams[10]; // 支持10级难度（下标0~9）
}
