﻿using StardewValley;
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
using StardewValley.Menus;
using StardewValley.Pathfinding;

namespace FarmCafe.Framework.Managers
{
    internal partial class CafeManager
    {
        internal List<GameLocation> CafeLocations;
        internal IList<Item> MenuItems = new List<Item>(new Item[27]);
        internal IList<Item> RecentlyAddedMenuItems = new List<Item>(new Item[9]);

        internal List<Visitor> CurrentVisitors = new List<Visitor>();
        internal List<string> CurrentNpcVisitors = new List<string>();
        internal NPC EmployeeNpc;
        internal List<Table> Tables = new();
        internal Dictionary<string, ScheduleData> NpcVisitorSchedules = new Dictionary<string, ScheduleData>();

        private readonly Dictionary<string, List<LocationWarpRoute>> RoutesToCafe = new Dictionary<string, List<LocationWarpRoute>>();

        internal Dictionary<Rectangle, List<Vector2>> MapTablesInCafeLocation = new Dictionary<Rectangle, List<Vector2>>();

        internal List<VisitorModel> VisitorModels = new List<VisitorModel>();
        internal List<string> VisitorModelsInUse = new List<string>();
        internal List<VisitorGroup> CurrentGroups = new List<VisitorGroup>();

        public Point BusPosition;

        internal int OpeningTime = 1200;
        internal int ClosingTime = 2100;
        internal int LastTimeVisitorsArrived;
        internal short VisitorGroupsDinedToday;

        public CafeManager()
        {
            CacheBusPosition();
        }

        internal void DayUpdate()
        {
            if (RoutesToCafe == null || RoutesToCafe.Count == 0)
                PopulateRoutesToCafe();

            PopulateTables(CafeLocations);
            LastTimeVisitorsArrived = OpeningTime;

            // Set which NPCs can visit today based on how many days it's been since their last visit, and their 
            // visit frequency level given in their visit data.
            foreach (var npcDataPair in NpcVisitorSchedules)
            {
                int daysSinceLastVisit = Game1.Date.TotalDays - npcDataPair.Value.LastVisitedDate.TotalDays;
                int daysAllowedBetweenVisits = npcDataPair.Value.Frequency switch
                {
                    0 => 200,
                    1 => 28,
                    2 => 15,
                    3 => 8,
                    4 => 2,
                    5 => 0,
                    _ => 999999
                };

                npcDataPair.Value.CanVisitToday = daysSinceLastVisit > daysAllowedBetweenVisits;
            }
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
            return MenuItems.Where(c => c != null).MinBy((i) => Game1.random.Next());
        }

        internal void CacheBusPosition()
        {
            var tiles = GetLocationFromName("BusStop").Map.GetLayer("Back").Tiles.Array;

            for (var i = 0; i < tiles.GetLength(0); i++)
            {
                for (var j = 0; j < tiles.GetLength(1); j++)
                {
                    if (tiles[i, j].Properties.ContainsKey("TouchAction") && tiles[i, j].Properties["TouchAction"] == "Bus")
                    {
                        BusPosition = new Point(i, j + 1);
                        return;
                    }
                }
            }
            
            Logger.Log("Couldn't find Bus position in Bus Stop", LogLevel.Warn);
            BusPosition = new Point(12, 10);
        }
    }
}