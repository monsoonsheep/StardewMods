using StardewValley;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.Linq;
using StardewCafe.Framework.Customers;
using StardewModdingAPI;
using xTile.Tiles;
using Point = Microsoft.Xna.Framework.Point;
using StardewValley.Menus;
using StardewValley.Pathfinding;
using StardewCafe.Framework.Objects;

namespace StardewCafe.Framework
{
    internal static partial class CafeManager
    {
        internal static List<GameLocation> CafeLocations;
        internal static IList<Item> MenuItems = new List<Item>(new Item[27]);
        internal static IList<Item> RecentlyAddedMenuItems = new List<Item>(new Item[9]);

        internal static List<Table> Tables = new();

        private static readonly Dictionary<string, List<LocationWarpRoute>> RoutesToCafe = new Dictionary<string, List<LocationWarpRoute>>();

        internal static Dictionary<Rectangle, List<Vector2>> MapTablesInCafeLocation = new Dictionary<Rectangle, List<Vector2>>();

        internal static int OpeningTime = 1200;
        internal static int ClosingTime = 2100;
        internal static int LastTimeCustomersArrived;

        internal static List<CustomerGroup> CurrentGroups = new List<CustomerGroup>();
        internal static List<NPC> CurrentNpcVisitors = new List<NPC>();
        internal static Dictionary<string, ScheduleData> NpcCustomerSchedule = new Dictionary<string, ScheduleData>();


        internal static void DayUpdate()
        {
            if (RoutesToCafe == null || RoutesToCafe.Count == 0)
                PopulateRoutesToCafe();

            PopulateTables(CafeLocations);
            LastTimeCustomersArrived = Game1.timeOfDay;
        }

        /// <summary>
        /// Add an item to the cafe menu
        /// </summary>
        public static bool AddToMenu(Item itemToAdd)
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

        public static Item RemoveFromMenu(int slotNumber)
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

        internal static IEnumerable<Item> GetMenuItems()
        {
            return MenuItems.Where(c => c != null);
        }

        internal static Item GetRandomItemFromMenu()
        {
            return MenuItems.Where(c => c != null).MinBy((i) => Game1.random.Next());
        }
    }
}