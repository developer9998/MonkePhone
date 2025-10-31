using BepInEx.Logging;

namespace MonkePhone.Tools;

public class Logging
{
    public static ManualLogSource Logger;

    public static void Info(object data) => Log(data);

    public static void Warning(object data) => Log(data, LogLevel.Warning);

    public static void Error(object data) => Log(data, LogLevel.Error);

    public static void Fatal(object data) => Log(data, LogLevel.Fatal);

    public static void Log(object data, LogLevel level = LogLevel.Info)
    {
#if DEBUG
        Logger.Log(level, data);
#endif
    }
}