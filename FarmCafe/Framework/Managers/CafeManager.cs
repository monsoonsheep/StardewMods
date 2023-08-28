using StardewValley;
using StardewValley.Locations;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using static FarmCafe.Framework.Utilities.Utility;
using StardewModdingAPI;
using FarmCafe.Framework.Characters;
using FarmCafe.Framework.Models;
using FarmCafe.Framework.Objects;
using Microsoft.Xna.Framework.Graphics;
using xTile.Tiles;
using Point = Microsoft.Xna.Framework.Point;
using FarmCafe.Locations;
using StardewValley.Buildings;
using StardewValley.Objects;

namespace FarmCafe.Framework.Managers
{
    internal partial class CafeManager
    {
        internal static List<GameLocation> CafeLocations = ModEntry.CafeLocations;
        internal static IList<Item> MenuItems = ModEntry.MenuItems;
        internal static List<Customer> CurrentCustomers = ModEntry.CurrentCustomers;
        internal static NPC HelperNpc;
        internal static List<Table> Tables = ModEntry.Tables;
        internal List<NPC> Basement;

        internal List<List<string>> RoutesToCafe;

        internal List<CustomerModel> CustomerModels = new List<CustomerModel>();
        internal List<CustomerModel> CustomerModelsInUse = new List<CustomerModel>();
        internal List<CustomerGroup> CurrentGroups;

        public Point BusPosition;

        internal int OpeningTime = 0800;
        internal int ClosingTime = 2100;
        internal int LastTimeCustomersArrived;
        internal short CustomerGroupsDinedToday;
        internal short NumberOfCustomerGroupsPresentRightNow;

        public CafeManager()
        {
            Basement = new List<NPC>();
            CurrentGroups = new List<CustomerGroup>();
            CacheBusPosition();
        }

        internal void CacheBusPosition()
        {
            Tile[,] tiles = GetLocationFromName("BusStop").Map.GetLayer("Back").Tiles.Array;
            for (var i = 0; i < tiles.GetLength(0); i++)
            for (var j = 0; j < tiles.GetLength(1); j++)
                if (tiles[i, j].Properties.ContainsKey("TouchAction") && tiles[i, j].Properties["TouchAction"] == "Bus")
                {
                    BusPosition = new Point(i, j + 1);
                    //Logger.Log($"bus position is {BusPosition}");
                    return;
                }

            Logger.Log("Couldn't find Bus position in Bus Stop", LogLevel.Warn);
            BusPosition = new Point(12, 10);
        }

        public bool AddToMenu(Item itemToAdd)
        {
            if (MenuItems.Contains(itemToAdd, new ItemEqualityComparer()))
                return false;
            
            for (int i = 0; i < MenuItems.Count; i++)
            {
                if (MenuItems[i] == null)
                {
                    MenuItems[i] = itemToAdd.getOne();
                    MenuItems[i].Stack = 1;
                    return true;
                }
            }

            return false;
        }

        public Item RemoveFromMenu(int slotNumber)
        {
            Item tmp = MenuItems[slotNumber];
            if (tmp == null)
                return null;
            
            MenuItems[slotNumber] = null;
            int firstEmpty = slotNumber;
            for (int i = slotNumber + 1; i < MenuItems.Count; i++)
            {
                if (MenuItems[i] != null)
                {
                    MenuItems[firstEmpty] = MenuItems[i];
                    MenuItems[i] = null;
                    firstEmpty += 1;
                }
            }

            return tmp;
        }

        internal Item GetRandomItemFromMenu()
        {
            return MenuItems.Where(c => c != null).OrderBy((i) => Game1.random.Next()).FirstOrDefault();
        }
    }

    public class ItemEqualityComparer : IEqualityComparer<Item>
    {
        public bool Equals(Item x, Item y)
        {
            return x != null && y != null && x.ParentSheetIndex == y.ParentSheetIndex;
        }

        public int GetHashCode(Item obj) => (obj != null) ? obj.ParentSheetIndex * 900 : -1;
    }

}