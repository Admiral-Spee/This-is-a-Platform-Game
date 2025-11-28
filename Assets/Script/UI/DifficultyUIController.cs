using UnityEngine;
using TMPro;
using System.Text;

public class DifficultyUIController : MonoBehaviour
{
    public TMP_Text displayText;

    // 只在被通知时刷新
    public void UpdateDisplay(LevelParams oldParams, LevelParams newParams)
    {
        if (!displayText || newParams == null) return;

        //当前难度等级
        StringBuilder sb = new StringBuilder();
        sb.Append($"{"Current difficulty level", -30}{newParams.difficultyLevel, 8}");
        sb.Append($"{GetMark(newParams.difficultyLevel, oldParams.difficultyLevel), 3}");
        sb.AppendLine();
        sb.AppendLine();

        //坑洞生成概率
        sb.Append($"{"Pit generation probability", -30}{newParams.pitChance.ToString("F2"), 8}");
        sb.Append($"{GetMark(newParams.pitChance, oldParams.pitChance),3}");
        sb.AppendLine();

        //平台生成概率
        sb.Append($"{"Platform probability", -30}{newParams.platformChance.ToString("F2"),8}");
        sb.Append($"{GetMark(newParams.platformChance, oldParams.platformChance),3}");
        sb.AppendLine();
        sb.AppendLine();

        //地刺生成概率
        sb.Append($"{"Spike generation probability", -30}{newParams.spikeChance.ToString("F2"),8}");
        sb.Append($"{GetMark(newParams.spikeChance, oldParams.spikeChance),3}");
        sb.AppendLine();

        //地刺停留时间
        sb.Append($"{"Spike stay time", -30}{newParams.spikeStayTime.ToString("F2"),8}");
        sb.Append($"{GetMark(newParams.spikeStayTime, oldParams.spikeStayTime),3}");
        sb.AppendLine();
        sb.AppendLine();

        //史莱姆生成概率
        sb.Append($"{"Slime generation probability", -30}{newParams.slimeChance.ToString("F2"),8}");
        sb.Append($"{GetMark(newParams.slimeChance, oldParams.slimeChance),3}");
        sb.AppendLine();

        //史莱姆速度
        sb.Append($"{"Slime speed", -30}{newParams.slimeSpeed.ToString("F2"),8}");
        sb.Append($"{GetMark(newParams.slimeSpeed, oldParams.slimeSpeed),3}");
        sb.AppendLine();
        sb.AppendLine();

        //炮塔生成概率
        sb.Append($"{"Turret generation probability", -30}{newParams.turretNums.ToString("F2"),8}");
        sb.Append($"{GetMark(newParams.turretNums, oldParams.turretNums),3}");
        sb.AppendLine();

        //炮塔射击范围
        sb.Append($"{"Turret Detect Range", -30}{newParams.turretDetectRange.ToString("F2"),8}");
        sb.Append($"{GetMark(newParams.turretDetectRange, oldParams.turretDetectRange),3}");
        sb.AppendLine();

        //炮塔准备时间
        sb.Append($"{"Turret Aim Time", -30}{newParams.turretAimTime.ToString("F2"),8}");
        sb.Append($"{GetMark(newParams.turretAimTime, oldParams.turretAimTime),3}");
        sb.AppendLine();

        displayText.text = sb.ToString();
    }

    string GetMark(float cur, float last, float eps = 1e-4f)
    {
        if (Mathf.Abs(cur - last) < eps) return " =";
        return cur > last ? " +" : " -";
    }
}
