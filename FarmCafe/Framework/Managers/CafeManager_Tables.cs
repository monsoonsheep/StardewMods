using FarmCafe.Framework.Objects;
using StardewModdingAPI;
using StardewValley.Objects;
using StardewValley;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using static FarmCafe.Framework.Utility;
using FarmCafe.Framework.Characters;

namespace FarmCafe.Framework.Managers
{
    internal partial class CafeManager
    {
        internal void PopulateTables(List<GameLocation> locations)
        {
            // TODO: Test whether some tables are still tracked even after they refer to an outdated CafeLocation
            foreach (var location in locations)
            {
                foreach (Furniture table in location.furniture)
                {
                    if (!IsTable(table)) continue;
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
                    TryAddTable(mapTable);
                }
            }
            FreeAllTables();
            Multiplayer.Sync.SyncTables();
        }
       
        internal bool TryAddTable(Table table, bool update = true)
        {
            if (table == null || table.Seats.Count == 0)
                return false;

            table.Free();
            Tables.Add(table);
            Logger.Log("Adding table");
            if (update)
                Multiplayer.Sync.SyncTables();
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
                Logger.Log("Trying to remove a table that isn't tracked", LogLevel.Warn);
            else
            {
                Logger.Log($"Table removed");
                Tables.Remove(table);
            }

            Multiplayer.Sync.SyncTables();
        }

        internal List<Table> GetFreeTables(int minimumSeats = 1)
        {
            return Tables.OrderBy(_ => Game1.random.Next()).Where(t => !t.IsReserved && t.Seats.Count >= minimumSeats).ToList();
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

        internal FurnitureTable TryAddFurnitureTable(Furniture table, GameLocation location)
        {
            FurnitureTable trackedTable = IsTableTracked(table, location);

            if (trackedTable == null)
            {
                trackedTable = new FurnitureTable(table, location)
                {
                    CurrentLocation = location
                };
                return TryAddTable(trackedTable) ? trackedTable : null;
            }

            return trackedTable;
        }

        internal void HandleFurnitureChanged(IEnumerable<Furniture> added, IEnumerable<Furniture> removed, GameLocation location)
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

                    FurnitureTable newTable = TryAddFurnitureTable(facingFurniture, location);
                    newTable?.AddChair(f);
                }
                else if (IsTable(f))
                {
                    TryAddFurnitureTable(f, location);
                }
            }
        }

        internal static void FarmerClickTable(Table table, Farmer who)
        {
            CustomerGroup groupOnTable =
                CurrentCustomers.FirstOrDefault(c => c.Group.ReservedTable == table)?.Group;

            if (groupOnTable == null)
            {
                Logger.Log("Didn't get group from table");
                return;
            }

            if (groupOnTable.Members.All(c => c.State.Value == CustomerState.OrderReady))
            {
                table.IsReadyToOrder = false;
                foreach (Customer customer in groupOnTable.Members)
                {
                    customer.StartWaitForOrder();
                }
            }
            else if (groupOnTable.Members.All(c => c.State.Value == CustomerState.WaitingForOrder))
            {
                foreach (Customer customer in groupOnTable.Members)
                {
                    if (customer.OrderItem != null && who.Items.ContainsId(customer.OrderItem.ItemId, 1))
                    {
                        Logger.Log($"Customer item = {customer.OrderItem.ParentSheetIndex}, inventory = {who.Items.ContainsId(customer.OrderItem.ItemId, 1)}");
                        customer.OrderReceive();
                        who.removeFirstOfThisItemFromInventory(customer.OrderItem.ItemId);
                    }
                }
            }
        }
    }
}
