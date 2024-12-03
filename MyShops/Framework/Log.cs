using StardewModdingAPI;

namespace StardewMods.MyShops.Framework;

internal static class Log
{
    internal static IMonitor Monitor = null!;

    public static void Debug(string message, LogLevel level = LogLevel.Debug)
    {
        Monitor.Log(message, level);
    }

    public static void Error(string message)
    {
        Monitor.Log(message, LogLevel.Error);
    }
    public static void Warn(string message)
    {
        Monitor.Log(message, LogLevel.Warn);
    }

    public static void Trace(string message)
    {
        Monitor.Log(message, LogLevel.Trace);
    }

    public static void Info(string message)
    {
        Monitor.Log(message, LogLevel.Info);
    }
}
