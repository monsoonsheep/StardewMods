using System.Linq;
using VisitorFramework;
using VisitorFramework.Framework.Characters;
using VisitorFramework.Framework.Managers;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.Objects;

namespace VisitorFramework
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
                    break;
                case SButton.NumPad1:
                    WarpToBus();
                    break;
                case SButton.NumPad2:
                    break;
                case SButton.NumPad3:
                    if (BusManager.BusGone)
                        BusManager.BusReturn();
                    else
                        BusManager.BusLeave();
                    break;
                case SButton.NumPad4:
                    ListVisitors();
                    break;
                case SButton.NumPad5:
                    Building sign = GetSignboardBuilding();
                    if (sign != null)
                    {
                        Logger.Log(sign.GetData().Size.ToString());
                    }
                    break;
                case SButton.NumPad6:
                    Logger.Log(Game1.player.ActiveObject?.ItemId);
                    Game1.player.addItemToInventory(new Furniture("1220", new Vector2(0,0)).getOne());
                    Game1.player.addItemToInventory(new Furniture("21", new Vector2(0,0)).getOne());
                    Game1.player.addItemToInventory(new Furniture("21", new Vector2(0, 0)).getOne());
                    break;
                case SButton.M:
                    Logger.Log("Breaking");
                    break;
                case SButton.N:
                    break;
                case SButton.V:
                    
                    break;
                default:
                    return;
            }
        }

        public static Building GetSignboardBuilding()
        {
            return Game1.getFarm().buildings.FirstOrDefault(b => b.buildingType.Value == "monsoonsheep.VisitorFramework_CafeSignboard");
        }

        public static void WarpToBus()
        {
            Game1.warpFarmer("BusStop", 12, 15, false);
        }

        
        internal static void ListVisitors()
        {
            Logger.Log("Characters in current");
            foreach (var ch in Game1.currentLocation.characters)
                if (ch is Visitor)
                    Logger.Log(ch.ToString());
                else
                    Logger.Log("NPC: " + ch.Name);

            foreach (var f in Game1.getFarm().furniture)
            {
                foreach (var pair in f.modData.Pairs) 
                    Logger.Log($"{pair.Key}: {pair.Value}");
                Logger.Log(f.modData.ToString());
            }
        }

    }
}
