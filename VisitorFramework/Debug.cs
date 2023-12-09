#region Usings
using System.Linq;
using VisitorFramework;
using VisitorFramework.Framework.Visitors;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.Objects;
using VisitorFramework.Framework;
using VisitorFramework.Framework.Managers;

#endregion

namespace VisitorFramework
{
    #if DEBUG
    internal static class Debug
    {
        public static void ButtonPress(object sender, ButtonPressedEventArgs e)
        {
            if (!Context.IsMainPlayer || !Context.CanPlayerMove)
                return;

            switch (e.Button)
            {
                case SButton.NumPad0:
                    //VisitorManager.SpawnVisitors();
                    break;
                case SButton.NumPad1:
                    WarpToBus();
                    break;
                case SButton.NumPad2:
                    VisitorManager.RemoveAllVisitors();
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
    #endif
}
