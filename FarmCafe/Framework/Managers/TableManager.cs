using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Objects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using FarmCafe.Framework.Objects;
using FarmCafe.Locations;
using StardewModdingAPI;
using xTile.Tiles;
using Utility = FarmCafe.Framework.Utilities.Utility;

namespace FarmCafe.Framework.Managers
{
    internal class TableManager
    {
        internal List<Table> Tables;

        internal bool FurnitureShouldBeUpdated = false;

        public TableManager(ref List<Table> tables)
        {
            Tables = tables;
        }

        internal void PopulateTables(List<GameLocation> locations)
        {
            foreach (var location in locations)
            {
                foreach (Furniture table in location.furniture)
                {
                    if (!Utility.IsTable(table)) continue;
                    // If we already have this table object registered
                    if (Tables.OfType<FurnitureTable>().Any(
                            t =>  t.Position == table.TileLocation && t.CurrentLocation.Name == location.Name))
                    {
                        continue;
                    }

                    FurnitureTable tableToAdd = new FurnitureTable(table, location)
                    {
                        CurrentLocation = location
                    };
                    TryAddTable(tableToAdd, update: false);
                }
            }

            // Make sure every table in TrackedTables satisfies the condition that
            // its location reference contains it in its furniture list
            Tables.RemoveAll(t => t is FurnitureTable && !t.CurrentLocation.furniture
                    .Any(f => f.TileLocation == t.Position));

            CafeLocation cafe = locations.OfType<CafeLocation>().FirstOrDefault();
            if (cafe != null)
            {
                foreach (var pair in cafe.MapTables)
                {
                    Rectangle newRectangle = new Rectangle(pair.Key.X * 64, pair.Key.Y * 64, pair.Key.Width * 64, pair.Key.Height * 64);
                    MapTable mapTable = new MapTable(newRectangle, cafe, pair.Value);
                    TryAddTable(mapTable);
                }
            }
            FreeAllTables();
            Multiplayer.SyncTables();
        }
       
        internal bool TryAddTable(Table table, bool update = true)
        {
            if (table == null || table.Seats.Count == 0)
                return false;

            table.Free();
            Tables.Add(table);
            Debug.Log("Adding table");
            if (update)
                Multiplayer.SyncTables();
            return true;
        }

        internal void RemoveTable(FurnitureTable table)
        {
            foreach (var seat in table.Seats)
            {
                var chair = (FurnitureChair) seat;
                chair.ActualChair.modData.Remove("FarmCafeChairIsReserved");
                chair.ActualChair.modData.Remove("FarmCafeChairTable");
            }

            table.ActualTable.modData.Remove("FarmCafeTableIsReserved");
            if (!Tables.Contains(table))
                Debug.Log("Trying to remove a table that isn't tracked", LogLevel.Warn);
            else
            {
                Debug.Log($"Table removed");
                Tables.Remove(table);
            }

            Multiplayer.SyncTables();
        }

        internal List<Table> GetFreeTables(int minimumSeats = 1)
        {
            return Tables.OrderBy((_) => Game1.random.Next()).Where(t => !t.IsReserved && t.Seats.Count >= minimumSeats).ToList();
        }

        internal static bool ChairIsReserved(Furniture chair)
        {
            return chair.modData.TryGetValue("FarmCafeChairIsReserved", out var val) && val == "T";
        }

        internal void FreeAllTables()
        {
            foreach (var table in Tables)
                table.Free();
        }

        internal Table GetTableAt(GameLocation location, Vector2 position)
        {
            return Tables.Where(t => t.CurrentLocation.Equals(location)).FirstOrDefault(table => table.BoundingBox.Contains(position));
        }
    }
}