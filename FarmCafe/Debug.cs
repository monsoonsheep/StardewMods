using FarmCafe.Framework.Characters;
using StardewValley;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FarmCafe.Framework.Locations;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using FarmCafe.Framework.Managers;
using Microsoft.Xna.Framework;
using StardewValley.Menus;
using StardewValley.Objects;

namespace FarmCafe
{
    internal class Debug
    {
        public static void ButtonPress(SButton button)
        {
            switch (button)
            {
                case SButton.B:
                    break;
                case SButton.NumPad0:
                    ModEntry.CafeManager.SpawnGroupAtBus();
                    break;
                case SButton.NumPad1:
                    Debug.WarpToBus();
                    break;
                case SButton.NumPad2:
                    ModEntry.CafeManager.RemoveAllCustomers();
                    break;
                case SButton.NumPad3:
                    if (ModEntry.CafeManager.CurrentGroups.Any())
                    {
                        ModEntry.CafeManager.WarpGroup(ModEntry.CafeManager.CurrentGroups.First(), Game1.getFarm(), new Point(78, 16));
                    }

                    break;
                case SButton.NumPad4:
                    Game1.activeClickableMenu = new CarpenterMenu();
                    Debug.ListCustomers();
                    break;
                case SButton.NumPad5:
                    ModEntry.CafeManager.VisitRegularNpc(Game1.getCharacterFromName("Shane"));
                    //CafeLocations.OfType<CafeLocation>()?.FirstOrDefault()?.PopulateMapTables();
                    //OpenCafeMenu();
                    //NPC helper = Game1.getCharacterFromName("Sebastian");
                    //helper.clearSchedule();
                    //helper.ignoreScheduleToday = true;
                    //Game1.warpCharacter(helper, "BusStop", CustomerManager.BusPosition);
                    //helper.HeadTowards(CafeManager.CafeLocations.First(), new Point(12, 18), 2);
                    //helper.eventActor = true;
                    break;
                case SButton.NumPad6:
                    Logger.Log(string.Join(", ", ModEntry.MenuItems.Select(i => i.DisplayName)));
                    break;
                case SButton.M:
                    Logger.Log("Breaking");
                    break;
                case SButton.N:
                    Logger.Log(Game1.MasterPlayer.ActiveObject?.ParentSheetIndex.ToString());
                    Game1.MasterPlayer.addItemToInventory(new Furniture(1220, new Vector2(0, 0)).getOne());
                    Game1.MasterPlayer.addItemToInventory(new Furniture(21, new Vector2(0, 0)).getOne());
                    break;
                case SButton.V:
                    NPC shane = Game1.getCharacterFromName("Shane");
                    
                    if (shane != null)
                    {
                        if (shane is Customer c)
                        {
                            ModEntry.CurrentCustomers.Remove(c);
                            c.Group?.ReservedTable?.Free();
                        }
                        Game1.warpCharacter(shane, Game1.player.currentLocation, Game1.player.getTileLocation() + new Vector2(0, -1));
                        ModEntry.CafeManager.VisitRegularNpc(Game1.getCharacterFromName("Shane"));
                    }
                    //CustomerGroup g = CafeManager.SpawnGroup(Game1.player.currentLocation,
                    //    Game1.player.getTileLocationPoint() + new Point(0, -1), 1);
                    //g?.Members?.First()?.GoToSeat();
                    break;
                default:
                    return;
            }
        }
        public static void WarpToBus()
        {
            Game1.warpFarmer("BusStop", 12, 15, false);
        }

        public static void WarpToCafe()
        {
            var cafe = ModEntry.CafeLocations.First(l => l is CafeLocation);
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
