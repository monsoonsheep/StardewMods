using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;

namespace CollapseOnFarmFix
{
    internal static class Log
    {
        internal static IMonitor Monitor = null!;

        public static void Debug(string message, LogLevel level = LogLevel.Debug)
        {
            Monitor.Log(message, level);
        }
    }
}
