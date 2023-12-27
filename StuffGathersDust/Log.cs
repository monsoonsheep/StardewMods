
using StardewModdingAPI;
using StardewValley;

namespace StuffGathersDust
{
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

        public static void LogWithHudMessage(string message)
        {
            Game1.addHUDMessage(new HUDMessage(message, 2000f));
            Monitor.Log(message, LogLevel.Debug);
        }
    }
}
