using FarmCafe.Framework.Characters;
using StardewValley;
using System.Linq;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using FarmCafe.Framework.Managers;
using Microsoft.Xna.Framework;
using StardewValley.Buildings;
using StardewValley.Objects;
using static FarmCafe.Framework.Utility;

namespace FarmCafe
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
                    ModEntry.CafeManager.TryVisitCustomers();
                    break;
                case SButton.NumPad1:
                    Debug.WarpToBus();
                    break;
                case SButton.NumPad2:
                    ModEntry.CafeManager.RemoveAllCustomers();
                    break;
                case SButton.NumPad3:
                    if (ModEntry.BusManager.BusGone)
                        ModEntry.BusManager.BusReturn();
                    else
                        ModEntry.BusManager.BusLeave();
                    break;
                case SButton.NumPad4:
                    Debug.ListCustomers();
                    break;
                case SButton.NumPad5:
                    Building sign = GetSignboardBuilding();
                    if (sign != null)
                    {
                        Logger.Log(sign.GetData().Size.ToString());
                    }
                    break;
                case SButton.NumPad6:
                    Logger.Log(string.Join(", ", ModEntry.CafeManager.MenuItems.Where(i => i != null).Select(i => i.DisplayName)));
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
                    NPC shane = Game1.getCharacterFromName("Shane");
                    
                    if (shane != null)
                    {
                        if (shane is Customer c)
                        {
                            ModEntry.CafeManager.CurrentCustomers.Remove(c);
                            c.Group?.ReservedTable?.Free();
                        }
                        Game1.warpCharacter(shane, Game1.player.currentLocation, Game1.player.Tile + new Vector2(0, -1));
                        //ModEntry.CafeManager.VisitRegularNpc(Game1.getCharacterFromName("Shane"));
                    }
                    //CustomerGroup g = CafeManager.CreateCustomerGroup(Game1.player.currentLocation,
                    //    Game1.player.getTileLocationPoint() + new Point(0, -1), 1);
                    //g?.Members?.First()?.GoToSeat();
                    break;
                default:
                    return;
            }
        }

        public static Building GetSignboardBuilding()
        {
            return Game1.getFarm().buildings.FirstOrDefault(b => b.buildingType.Value == "monsoonsheep.FarmCafe_CafeSignboard");
        }

        public static void WarpToBus()
        {
            Game1.warpFarmer("BusStop", 12, 15, false);
        }

        public static void WarpToCafe()
        {
            var cafe = ModEntry.CafeManager.CafeLocations.First(IsLocationCafe);
            var warp = cafe.warps.First();
            Game1.warpFarmer(cafe.Name, warp.X, warp.Y - 1, 0);
        }

        internal static void ListCustomers()
        {
            Logger.Log("Characters in current");
            foreach (var ch in Game1.currentLocation.characters)
                if (ch is Customer)
                    Logger.Log(ch.ToString());
                else
                    Logger.Log("NPC: " + ch.Name);

            Logger.Log("Current customers: ");
            foreach (var customer in ModEntry.CafeManager.CurrentCustomers) 
                Logger.Log(customer.ToString());

            Logger.Log("Current models: ");
            foreach (var model in ModEntry.CafeManager.CustomerModels) 
                Logger.Log(model.ToString());

            foreach (var f in Game1.getFarm().furniture)
            {
                foreach (var pair in f.modData.Pairs) 
                    Logger.Log($"{pair.Key}: {pair.Value}");
                Logger.Log(f.modData.ToString());
            }
        }

    }
}
