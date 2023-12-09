#region Usings

using StardewModdingAPI;
using StardewValley;

#endregion

namespace VisitorFramework;

internal static class Log
{
    internal static IMonitor Monitor;

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

    public static void LogWithHudMessage(string message)
    {
        Game1.addHUDMessage(new HUDMessage(message, 2000f));
        Monitor.Log(message, LogLevel.Debug);
    }
}