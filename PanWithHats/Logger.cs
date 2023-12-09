
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;

namespace PanWithHats
{
    internal static class Logger
    {
        internal static IMonitor Monitor = ModEntry.Monitor;

        public static void Log(string message, LogLevel level = LogLevel.Debug)
        {
            Monitor.Log(message, level);
        }

        public static void LogWithHudMessage(string message)
        {
            Game1.addHUDMessage(new HUDMessage(message, Color.White, 2000));
            Monitor.Log(message, LogLevel.Debug);
        }
    }
}
