using System.Linq;
using Microsoft.Xna.Framework;
using MyCafe.Framework.Customers;
using MyCafe.Framework.Managers;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.Objects;

namespace MyCafe
{
    internal class Debug
    {
        public static void ButtonPress(object sender, ButtonPressedEventArgs e)
        {
            if (!Context.IsMainPlayer || !Context.CanPlayerMove)
                return;

            switch (e.Button)
            {
                case SButton.NumPad0:
                    CustomerManager.Instance?.SpawnCustomerOnRoad();
                    break;
                case SButton.NumPad1:
                    WarpToBus();
                    break;
                default:
                    return;
            }
        }

        public static void WarpToBus()
        {
            Game1.warpFarmer("BusStop", 12, 15, false);
        }
    }
}
