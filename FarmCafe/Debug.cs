using FarmCafe.Framework.Characters;
using FarmCafe.Framework.Managers;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;

namespace FarmCafe
{
	internal static class Debug
	{
		internal static IMonitor Monitor = FarmCafe.Monitor;

		public static void Debug_warpToBus()
		{
			Game1.warpFarmer("BusStop", 12, 15, false);
		}

		public static void Log(string message)
		{
			Monitor.Log(message, LogLevel.Debug);
		}

		public static void Log(string message, LogLevel level)
		{
			Monitor.Log(message, level);
		}

        public static void LogWithHudMessage(string message)
        {
			Game1.addHUDMessage(new HUDMessage(message, Color.White, 2000));
            Monitor.Log(message, LogLevel.Debug);
        }

        internal static void Debug_ListCustomers()
        {
            Debug.Log("Characters in current");
            foreach (var ch in Game1.currentLocation.characters)
                if (ch is Customer)
                    Debug.Log(ch.ToString());
                else
                    Debug.Log("NPC: " + ch.Name);

            Debug.Log("Current customers: ");
            foreach (var customer in FarmCafe.CafeManager.CurrentCustomers) 
                Debug.Log(customer.ToString());

            Debug.Log("Current models: ");
            foreach (var model in FarmCafe.CafeManager.CustomerModels) 
                Debug.Log(model.ToString());

            foreach (var f in Game1.getFarm().furniture)
            {
                foreach (var pair in f.modData.Pairs) 
                    Debug.Log($"{pair.Key}: {pair.Value}");
                Debug.Log(f.modData.ToString());
            }
        }
    }
}
