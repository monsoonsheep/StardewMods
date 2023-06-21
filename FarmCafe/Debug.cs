using FarmCafe.Framework.Customers;
using FarmCafe.Framework.Managers;
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



		internal static void MoveDebugAll(int dir)
		{
			foreach (var c in CustomerManager.CurrentCustomers)
			{
				c.MoveDebug(dir);
			}
		}

		internal static void MoveDebug(this Customer me, int direction)
		{
			//switch (direction)
			//{
			// case 0:
			//  me.drawOffset += new Vector2(0, -1);
			//  break;
			// case 1:
			//  me.drawOffset += new Vector2(1, 0);
			//  break;
			// case 2:
			//  me.drawOffset += new Vector2(0, 1);
			//  break;
			// case 3:
			//  me.drawOffset += new Vector2(-1, 0);
			//  break;
			//}

			me.MovePosition(Game1.currentGameTime, Game1.viewport, Game1.getFarm());
			me.Halt();

			float seatback = (me.Seat.boundingBox.Value.Top + 16) / 10000f;
			float seatfront = (me.Seat.boundingBox.Value.Bottom - 8) / 10000f;
			float charlayer = me.getStandingY() / 10000f;

			Debug.Log($"seat back is {seatback}");
			Debug.Log($"seat front is {seatfront}");
			Debug.Log($"character layer is {charlayer}");
			Debug.Log($"{seatback} --- {charlayer} --- {seatfront}");
			Debug.Log($"character bouding box {me.GetBoundingBox()}");
			Debug.Log($"seat bounding box is {me.Seat.boundingBox}");
		}

	}

}
