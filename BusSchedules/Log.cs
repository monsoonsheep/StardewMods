#region Usings

using StardewModdingAPI;
using StardewValley;

#endregion

namespace BusSchedules;

internal static class Log
{
    internal static IMonitor Monitor;

    internal static void Debug(string message)
    {
        Monitor.Log(message, LogLevel.Debug);
    }

    internal static void Info(string message)
    {
        Monitor.Log(message, LogLevel.Info);
    }

    internal static void Error(string message)
    {
        Monitor.Log(message, LogLevel.Error);
    }

    internal static void Warn(string message)
    {
        Monitor.Log(message, LogLevel.Warn);
    }

    internal static void Trace(string message)
    {
        Monitor.Log(message, LogLevel.Trace);
    }

    internal static void LogWithHudMessage(string message)
    {
        Game1.addHUDMessage(new HUDMessage(message, 2000f));
        Monitor.Log(message, LogLevel.Debug);
    }
}