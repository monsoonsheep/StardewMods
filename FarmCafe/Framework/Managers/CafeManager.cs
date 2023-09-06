using StardewValley;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.Linq;
using static FarmCafe.Framework.Utility;
using StardewModdingAPI;
using FarmCafe.Framework.Characters;
using FarmCafe.Framework.Objects;
using xTile.Tiles;
using Point = Microsoft.Xna.Framework.Point;
using FarmCafe.Models;
using StardewValley.Pathfinding;

namespace FarmCafe.Framework.Managers
{
    internal partial class CafeManager
    {
        internal static List<GameLocation> CafeLocations;
        internal static IList<Item> MenuItems = new List<Item>(new Item[27]);
        internal static IList<Item> RecentlyAddedMenuItems = new List<Item>(new Item[9]);

        internal static List<Customer> CurrentCustomers = new List<Customer>();
        internal List<string> CurrentNpcCustomers = new List<string>();
        internal static NPC EmployeeNpc;
        internal static List<Table> Tables = new();
        internal static Dictionary<string, ScheduleData> NpcCustomerSchedules = new Dictionary<string, ScheduleData>();

        private static readonly Dictionary<string, List<LocationWarpRoute>> RoutesToCafe = new Dictionary<string, List<LocationWarpRoute>>();

        internal Dictionary<Rectangle, List<Vector2>> MapTablesInCafeLocation = new Dictionary<Rectangle, List<Vector2>>();

        internal List<CustomerModel> CustomerModels = new List<CustomerModel>();
        internal List<string> CustomerModelsInUse = new List<string>();
        internal List<CustomerGroup> CurrentGroups = new List<CustomerGroup>();

        public Point BusPosition;

        internal int OpeningTime = 1200;
        internal int ClosingTime = 2100;
        internal int LastTimeCustomersArrived;
        internal short CustomerGroupsDinedToday;

        public CafeManager()
        {
            CacheBusPosition();
        }

        internal void DayUpdate()
        {
            if (RoutesToCafe == null || RoutesToCafe.Count == 0)
                PopulateRoutesToCafe();

            PopulateTables(CafeLocations);
            LastTimeCustomersArrived = OpeningTime;

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

        internal IEnumerable<Item> GetMenuItems()
        {
            return MenuItems.Where(c => c != null);
        }

        internal Item GetRandomItemFromMenu()
        {
            return MenuItems.Where(c => c != null).OrderBy((i) => Game1.random.Next()).FirstOrDefault();
        }
    }
}