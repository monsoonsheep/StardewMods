using StardewModdingAPI;
using StardewValley.Objects;
using StardewValley;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using StardewCafe.Framework.Objects;

namespace StardewCafe.Framework
{
    internal static partial class CafeManager
    {
        /// <summary>
        /// Add furniture-based and map-based tables from all registered Cafe Locations. Only the tables with at least one chair are added
        /// </summary>
        internal static void PopulateTables(List<GameLocation> locations)
        {
            int count = 0;
            // TODO: Test whether some tables are still tracked even after they refer to an outdated CafeLocation
            foreach (var location in locations)
            {
                foreach (Furniture table in location.furniture)
                {
                    if (!IsTable(table)) 
                        continue;

                    // If we already have this table object registered, skip
                    if (Tables.OfType<FurnitureTable>().Any(
                            t =>  t.Position == table.TileLocation && t.CurrentLocation.Name == location.Name))
                    {
                        continue;
                    }

                    FurnitureTable tableToAdd = new FurnitureTable(table, location)
                    {
                        CurrentLocation = location
                    };

                    if (TryAddTable(tableToAdd))
                        count++;
                }
            }
            Logger.Log($"{count} new furniture tables found in cafe locations.");
            count = 0;

            // Make sure every table in TrackedTables satisfies the condition that
            // its location reference contains it in its furniture list
            Tables.RemoveAll(t => t is FurnitureTable && !t.CurrentLocation.furniture
                    .Any(f => f.TileLocation == t.Position));

            // Remove all tables whose location isn't being tracked
            Tables.RemoveAll(t => !locations.Contains(t.CurrentLocation));

            // Populate Map tables
            GameLocation cafe = locations.FirstOrDefault(IsLocationCafe);
            if (cafe != null)
            {
                foreach (var pair in MapTablesInCafeLocation)
                {
                    Rectangle newRectangle = new Rectangle(pair.Key.X * 64, pair.Key.Y * 64, pair.Key.Width * 64, pair.Key.Height * 64);
                    MapTable mapTable = new MapTable(newRectangle, cafe, pair.Value);
                    if (TryAddTable(mapTable))
                        count++;
                }
            }
            Logger.Log($"{count} new map-based tables found in cafe locations.");

            FreeAllTables();
            Multiplayer.Sync.SyncTables();
        }

        /// <summary>
        /// Add a <see cref="Table"/> to the Tables list, only if it has at least one chair facing it next to it. 
        /// </summary>
        internal static bool TryAddTable(Table table)
        {
            if (table.Seats.Count == 0)
                return false;

            table.Free();
            Tables.Add(table);
            return true;
        }

        /// <summary>
        /// Remove a table from tracking. Called when a farmer breaks a furniture table or breaks a chair and its corresponding table doesn't have any more chairs left
        /// </summary>
        internal static void RemoveTable(FurnitureTable table)
        {
            foreach (var seat in table.Seats)
            {
                FurnitureChair chair = (FurnitureChair) seat;
                chair.ActualChair.modData.Remove(ModKeys.MODDATA_CHAIRRESERVED);
                chair.ActualChair.modData.Remove(ModKeys.MODDATA_CHAIRTABLE);
            }

            table.ActualTable.modData.Remove(ModKeys.MODDATA_TABLERESERVED);
            if (!Tables.Contains(table))
                Logger.Log("Trying to remove a table that isn't tracked", LogLevel.Warn);
            else
            {
                Logger.Log($"Table removed");
                Tables.Remove(table);
            }

            Multiplayer.Sync.SyncTables();
        }

        /// <summary>
        /// Return a list of <see cref="Table"/>s that aren't reserved
        /// </summary>
        internal static List<Table> GetFreeTables(int minimumSeats = 1)
        {
            return Tables.OrderBy(_ => Game1.random.Next()).Where(t => !t.IsReserved && t.Seats.Count >= minimumSeats).ToList();
        }

        /// <summary>
        /// Return a table that's free
        /// </summary>
        /// <param name="seatCount">Minimum number of seats required</param>
        /// <returns></returns>
        internal static Table GetFreeTable(int seatCount = 0)
        {
            var tables = GetFreeTables(seatCount);
            if (tables.Count == 0)
                return null;

            Table table;
            if (seatCount == 0)
            {
                // If memberCount not specified, calculate how many members it should be based on
                // the number of chairs on the table
                table = tables.First();
                int countSeats = table.Seats.Count;
                seatCount = countSeats switch
                {
                    1 => 1,
                    2 => Game1.random.Next(2) == 0 ? 2 : 1,
                    <= 4 => Game1.random.Next(countSeats) == 0 ? 2 : Game1.random.Next(3, countSeats + 1),
                    _ => Game1.random.Next(3, countSeats + 1)
                };
            }
            else
            {
                // If memberCount is specified, get the table that has the least chairs that can fit this group
                table = tables.OrderBy(t => t.Seats.Count).First();
            }

            return table;
        }

        /// <summary>
        /// Returns true if the given furniture (assumed a chair) is reserved by a Visitor
        /// </summary>
        internal static bool ChairIsReserved(Furniture chair)
        {
            return chair.modData.TryGetValue(ModKeys.MODDATA_CHAIRRESERVED, out var val) && val == "T";
        }

        /// <summary>
        /// Remove the reserved flags for all tracked tables
        /// </summary>
        internal static void FreeAllTables() => Tables.ForEach(t => t.Free());
        
        /// <summary>
        /// Get the <see cref="Table"/> (furniture or map-based) located at the given pixel position in the given location
        /// </summary>
        internal static Table GetTableAt(GameLocation location, Vector2 position)
        {
            return Tables.Where(t => t.CurrentLocation.Equals(location)).FirstOrDefault(table => table.BoundingBox.Contains(position));
        }

        /// <summary>
        /// Add a furniture table to the tracked list, after a farmer either places down a table or a chair. Called when after the placement, there is now a table that has at least one chair.
        /// </summary>
        internal static FurnitureTable TryAddFurnitureTable(Furniture table, GameLocation location)
        {
            FurnitureTable newTable = new FurnitureTable(table, location)
            {
                CurrentLocation = location
            };

            return TryAddTable(newTable) 
                ? newTable : null;
        }

        /// <summary>
        /// Add and/or remove furniture tables when the FurnitureListChanged Event fires. If a chair is added, its table is located and if not found, the table is also added
        /// </summary>
        internal static void HandleFurnitureChanged(IEnumerable<Furniture> added, IEnumerable<Furniture> removed, GameLocation location)
        {
            foreach (var f in removed)
            {
                if (IsChair(f))
                {
                    FurnitureChair trackedChair = Tables
                        .OfType<FurnitureTable>()
                        .SelectMany(t => t.Seats)
                        .OfType<FurnitureChair>()
                        .FirstOrDefault(seat => seat.Position == f.TileLocation && seat.Table.CurrentLocation.Equals(location));

                    if (trackedChair?.Table is not FurnitureTable table)
                        continue;

                    if (table.IsReserved)
                        Logger.Log("Removed a chair but the table was reserved");

                    table.RemoveChair(f);
                }
                else if (IsTable(f))
                {
                    FurnitureTable trackedTable = IsTableTracked(f, location);

                    if (trackedTable != null)
                    {
                        RemoveTable(trackedTable);
                    }
                }
            }
            foreach (var f in added)
            {
                if (IsChair(f))
                {
                    // Get position of table in front of the chair
                    Vector2 tablePos = f.TileLocation + (DirectionIntToDirectionVector(f.currentRotation.Value) * new Vector2(1, -1));

                    // Get table Furniture object
                    Furniture facingFurniture = location.GetFurnitureAt(tablePos);

                    if (facingFurniture == null ||
                        !IsTable(facingFurniture) ||
                        facingFurniture
                            .GetBoundingBox()
                            .Intersects(f.boundingBox.Value)) // if chair was placed on top of the table
                    {
                        continue;
                    }

                    FurnitureTable table = IsTableTracked(facingFurniture, location) 
                                           ?? TryAddFurnitureTable(facingFurniture, location);
                    table.AddChair(f);
                }
                else if (IsTable(f))
                {
                    FurnitureTable table = IsTableTracked(f, location);
                    if (table == null)
                        TryAddFurnitureTable(f, location);
                }
            }
        }

        /// <summary>
        /// Execute the function for a farmer clicking a table (map-based or furniture), like for taking a table's order or serving their food
        /// </summary>
        internal static void FarmerClickTable(Table table, Farmer who)
        {
            VisitorGroup groupOnTable =
                CurrentVisitors.FirstOrDefault(c => c.Group.ReservedTable == table)?.Group;

            if (groupOnTable == null)
            {
                Logger.Log("Didn't get group from table");
                return;
            }

            if (groupOnTable.Members.All(c => c.State.Value == VisitorState.OrderReady))
            {
                table.IsReadyToOrder = false;
                foreach (Visitor Visitor in groupOnTable.Members)
                {
                    Visitor.StartWaitForOrder();
                }
            }
            else if (groupOnTable.Members.All(c => c.State.Value == VisitorState.WaitingForOrder))
            {
                foreach (Visitor Visitor in groupOnTable.Members)
                {
                    if (Visitor.OrderItem != null && who.Items.ContainsId(Visitor.OrderItem.ItemId, 1))
                    {
                        Logger.Log($"Visitor item = {Visitor.OrderItem.ParentSheetIndex}, inventory = {who.Items.ContainsId(Visitor.OrderItem.ItemId, 1)}");
                        Visitor.OrderReceive();
                        who.removeFirstOfThisItemFromInventory(Visitor.OrderItem.ItemId);
                    }
                }
            }
        }
    }
}
