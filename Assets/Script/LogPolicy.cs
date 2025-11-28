// LogPolicy.cs （放到任意 Scripts 文件夹）
using UnityEngine;

public static class LogPolicy
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Apply()
    {
        // 按需选择一个策略（示例给三档，二选一/三选一即可）

#if UNITY_EDITOR
        // 编辑器下保留所有日志，便于调试
        Debug.unityLogger.logEnabled = true;
        Debug.unityLogger.filterLogType = LogType.Log; // 全开
        Application.SetStackTraceLogType(LogType.Log,       StackTraceLogType.ScriptOnly);
        Application.SetStackTraceLogType(LogType.Warning,   StackTraceLogType.ScriptOnly);
        Application.SetStackTraceLogType(LogType.Error,     StackTraceLogType.Full);
        Application.SetStackTraceLogType(LogType.Exception, StackTraceLogType.Full);
#else
#if DEVELOPMENT_BUILD
        // 开发构建：保留 Warning/Error，屏蔽普通 Log
        Debug.unityLogger.logEnabled = true;
        Debug.unityLogger.filterLogType = LogType.Warning;  // 仅警告/错误
        Application.SetStackTraceLogType(LogType.Log,       StackTraceLogType.None);
        Application.SetStackTraceLogType(LogType.Warning,   StackTraceLogType.ScriptOnly);
        Application.SetStackTraceLogType(LogType.Error,     StackTraceLogType.Full);
        Application.SetStackTraceLogType(LogType.Exception, StackTraceLogType.Full);
#else
        // 正式版：只保留 Error/Exception（或直接全关）
        Debug.unityLogger.logEnabled = false;
        Application.SetStackTraceLogType(LogType.Log, StackTraceLogType.None);
        Application.SetStackTraceLogType(LogType.Warning, StackTraceLogType.None);
        Application.SetStackTraceLogType(LogType.Error, StackTraceLogType.None);
        Application.SetStackTraceLogType(LogType.Exception, StackTraceLogType.None);

        // 如果你要彻底关闭所有脚本日志（连错误也不要）：
        // Debug.unityLogger.logEnabled = false;
#endif
#endif
    }
}
