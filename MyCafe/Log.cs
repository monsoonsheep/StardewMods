using StardewModdingAPI;
using StardewValley;

namespace MyCafe
{
	internal static class Log
	{
		internal static IMonitor Monitor = ModEntry.Monitor;

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

        public static void LogWithHudMessage(string message)
        {
			Game1.addHUDMessage(new HUDMessage(message, 2000));
            Monitor.Log(message, LogLevel.Debug);
        }
    }
}
