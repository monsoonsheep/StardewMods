using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Objects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using StardewModdingAPI;
using Utility = FarmCafe.Framework.Utilities.Utility;

namespace FarmCafe.Framework.Managers
{
    internal class TableManager
    {
        internal Dictionary<Furniture, GameLocation> TrackedTables;
        internal bool FurnitureShouldBeUpdated = false;

        public TableManager(ref Dictionary<Furniture, GameLocation> tables)
        {
            TrackedTables = tables;
        }

        internal void PopulateTables(List<GameLocation> locations)
        {
            foreach (var location in locations)
            {
                foreach (Furniture table in location.furniture)
                {
                    if (!Utility.IsTable(table)) continue;
                    // If we already have this table object registered
                    if (TrackedTables.Any(
                            t => t.Key.TileLocation == table.TileLocation && t.Value.Name == location.Name))
                    {
                        continue;
                    }

                    TryAddTable(table, location, update: false);

                }
            }

            //if (TrackedTables.Count - alreadyAddedCount - addedCount != 0)
            //{

            // Make sure every table in TrackedTables satisfies the condition that
            // its location reference contains it in its furniture list
            TrackedTables = TrackedTables
                .Where(pair => pair.Value.furniture
                    .Contains(pair.Key))
                .ToDictionary(x => x.Key, x => x.Value);

            FreeAllTables();
            Multiplayer.SyncTables();
        }

        internal bool TryAddTable(Furniture table, GameLocation location, bool update = true)
        {
            List<Furniture> foundChairs = FindChairsAroundTable(table, location);
            if (foundChairs.Count == 0)
                return false;

            table.modData["FarmCafeTable"] = "T";
            UpdateChairsForTable(table, foundChairs);

            Debug.Log($"Table added. {foundChairs.Count} chairs.");
            TrackedTables.Add(table, location);
            if (update)
                Multiplayer.SyncTables();
            return true;
        }

        internal void TryRemoveTable(Furniture table)
        {
            foreach (var chair in GetChairsOfTable(table))
            {
                chair.modData.Remove("FarmCafeChairIsReserved");
                chair.modData.Remove("FarmCafeChairTable");
            }

            table.modData.Remove("FarmCafeTable");
            table.modData.Remove("FarmCafeTableChairs");
            table.modData.Remove("FarmCafeTableIsReserved");

            Debug.Log($"Table removed");

            if (!TrackedTables.ContainsKey(table))
                Debug.Log("Trying to remove a table that isn't tracked", LogLevel.Warn);
            else
                TrackedTables.Remove(table);

            Multiplayer.SyncTables();
        }

        internal void UpdateChairsForTable(Furniture table, List<Furniture> chairs = null)
        {
            chairs ??= FindChairsAroundTable(table, TrackedTables[table]);

            string chairsString = "";
            foreach (Furniture chair in chairs)
            {
                chair.modData["FarmCafeChairIsReserved"] = "F";
                chair.modData["FarmCafeChairTable"] = $"{table.TileLocation.X} {table.TileLocation.Y}";
                chairsString += $"{chair.TileLocation.X} {chair.TileLocation.Y} ";
            }

            table.modData["FarmCafeTableChairs"] = chairsString;
        }

        internal static List<Furniture> FindChairsAroundTable(Furniture table, GameLocation location)
        {
            int sizeY = table.getTilesHigh();
            int sizeX = table.getTilesWide();
            Point pos = table.TileLocation.ToPoint();
            var chairs = new List<Furniture>();

            foreach (int i in new[] { -1, sizeY })
            {
                for (int j = 0; j < sizeX; j++)
                {
                    var chairAt = location.GetFurnitureAt(new Vector2(pos.X + j, pos.Y + i));
                    if (!Utility.IsChair(chairAt)) continue;
                    int rotation = chairAt.currentRotation.Value;
                    if (rotation == 0 && i == -1 || rotation == 2 && i != -1)
                        chairs.Add(chairAt);
                }
            }

            foreach (int i in new[] { -1, sizeX })
            {
                for (int j = 0; j < sizeY; j++)
                {
                    var chairAt = location.GetFurnitureAt(new Vector2(pos.X + i, pos.Y + j));
                    if (!Utility.IsChair(chairAt)) continue;
                    int rotation = chairAt.currentRotation.Value;
                    if (rotation == 1 && i == -1 || rotation == 3 && i != -1)
                        chairs.Add(chairAt);
                }
            }

            return chairs;
        }

        internal bool AddChairToTable(Furniture chair, Furniture table)
        {
            if (TableIsReserved((table)))
                return false;

            List<Furniture> chairs = GetChairsOfTable(table);
            if (chairs.Contains(chair))
            {
                Debug.Log("Table already knows the chair that was placed?", LogLevel.Warn);
                return false;
            }

            Debug.Log("Adding chair to table");

            chairs.Add(chair);
            UpdateChairsForTable(table, chairs);
            return true;
        }

        internal void TryRemoveChairFromTable(Furniture chair, Furniture table)
        {
            List<Furniture> chairs = GetChairsOfTable(table);
            if (!chairs.Contains(chair))
            {
                Debug.Log("Chair was removed but the table in front of it didn't recognize it before.!",
                    LogLevel.Error);
                return;
            }

            chair.modData["FarmCafeChairIsReserved"] = "F";
            chair.modData.Remove("FarmCafeChairTable");

            Debug.Log("Removing chair from table");

            chairs.Remove(chair);
            UpdateChairsForTable(table, chairs);

            if (chairs.Count == 0)
                TryRemoveTable(table);
        }

        internal List<Furniture> GetChairsOfTable(Furniture table)
        {
            var chairs = new List<Furniture>();
            if (!TrackedTables.ContainsKey(table))
                return chairs;

            table.modData.TryGetValue("FarmCafeTableChairs", out string chairsString);
            if (string.IsNullOrEmpty(chairsString))
                return chairs;


            GameLocation location = TrackedTables[table];
            foreach (Match match in Regex.Matches(chairsString, @"\d+\s\d+").ToList())
            {
                if (!match.Success) continue;

                var tile = match.ToString().Split(' ').Select(int.Parse).ToArray();
                Furniture chair = location.GetFurnitureAt(new Vector2(tile[0], tile[1]));
                if (chair == null)
                {
                    Debug.Log("Bad chair registered in table modData", LogLevel.Error);
                    continue;
                }

                chairs.Add(chair);
            }

            return chairs;
        }

        internal Furniture TryReserveTable(int minimumSeats = 1)
        {
            return (from table in TrackedTables.OrderBy(x => Game1.random.Next())
                    where !TableIsReserved(table.Key)
                    let chairs = GetChairsOfTable(table.Key)
                    where chairs?.Count >= minimumSeats
                    select table.Key)
                .FirstOrDefault();
        }

        internal static bool TableIsReserved(Furniture table)
        {
            return table.modData.TryGetValue("FarmCafeTableIsReserved", out var val) && val == "T";
        }

        internal static bool ChairIsReserved(Furniture chair)
        {
            return chair.modData.TryGetValue("FarmCafeChairIsReserved", out var val) && val == "T";
        }

        public void FreeTable(Furniture table)
        {
            table.modData["FarmCafeTableIsReserved"] = "F";
            foreach (Furniture chair in GetChairsOfTable(table))
            {
                Debug.Log("Freeing chair" + chair.HasSittingFarmers());
                chair.modData["FarmCafeChairIsReserved"] = "F";
            }

            UpdateChairsForTable(table);
        }

        public void FreeAllTables()
        {
            foreach (var table in TrackedTables.Keys)
            {
                FreeTable(table);
            }
        }
    }
}