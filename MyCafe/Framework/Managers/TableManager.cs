using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MyCafe.Framework.Objects;
using StardewModdingAPI.Events;
using StardewModdingAPI;
using StardewValley.Objects;
using StardewValley;
using StardewValley.Buildings;
using xTile.Layers;
using xTile.ObjectModel;
using xTile.Tiles;

namespace MyCafe.Framework.Managers
{
    internal sealed class TableManager
    {
        internal static TableManager Instance;

        internal readonly List<Table> CurrentTables;
        internal readonly Dictionary<Rectangle, List<Vector2>> MapTablesInCafeLocation;

        internal TableManager()
        {
            Instance = this;
            CurrentTables = new List<Table>();
            MapTablesInCafeLocation = new Dictionary<Rectangle, List<Vector2>>();
        }

        internal void PopulateTables(GameLocation exterior, GameLocation interior = null)
        {
            int count = 0;
            var locations = new List<GameLocation>(){exterior};
            if (interior != null)
                locations.Add(interior);

            // TODO: Test whether some tables are still tracked even after they refer to an outdated CafeLocation
            foreach (var location in locations)
            {
                foreach (Furniture furniture in location.furniture)
                {
                    if (!Utility.IsTable(furniture))
                        continue;

                    // If we already have this table object registered, skip
                    if (CurrentTables.OfType<FurnitureTable>().Any(
                            t => t.Position == furniture.TileLocation && t.CurrentLocation == location.Name))
                    {
                        continue;
                    }

                    FurnitureTable tableToAdd = new FurnitureTable(furniture, location.Name);

                    if (TryAddTable(tableToAdd))
                        count++;
                }
            }

            if (count > 0) {
                Log.Debug($"{count} new furniture tables found in cafe locations.");
                count = 0;
            }

            // Remove duplicate tables
            for (var i = CurrentTables.Count - 1; i >= 0; i--)
            {
                GameLocation location = Utility.GetLocationFromName(CurrentTables[i].CurrentLocation);
                if ((CurrentTables[i] is FurnitureTable && !location.furniture.Any(f => f.TileLocation == CurrentTables[i].Position)) ||
                    locations.Any(l => CurrentTables[i].CurrentLocation == l.Name))
                {
                    CurrentTables.RemoveAt(i);
                }
            }

            // Populate Map tables for cafe indoors
            if (interior != null) {
                PopulateMapTables(interior);
                foreach (var pair in MapTablesInCafeLocation)
                {
                    Rectangle newRectangle = new Rectangle(pair.Key.X * 64, pair.Key.Y * 64, pair.Key.Width * 64, pair.Key.Height * 64);
                    MapTable mapTable = new MapTable(newRectangle, interior.Name, pair.Value);
                    if (TryAddTable(mapTable))
                        count++;
                }
                Log.Debug($"{count} new map-based tables found in cafe locations.");
            }

            FreeAllTables();
        }


        internal void PopulateMapTables(GameLocation indoors)
        {
            if (MapTablesInCafeLocation?.Count != 0)
                return;
            MapTablesInCafeLocation.Clear();
            Layer layer = indoors.Map.GetLayer("Buildings");

            Dictionary<string, Rectangle> seatStringToTableRecs = new();

            for (int i = 0; i < layer.LayerWidth; i++)
            {
                for (int j = 0; j < layer.LayerHeight; j++)
                {
                    Tile tile = layer.Tiles[i, j];
                    if (tile == null)
                        continue;

                    if (tile.TileIndexProperties.TryGetValue(ModKeys.MAPSEATS_TILEPROPERTY, out var val)
                        || tile.Properties.TryGetValue(ModKeys.MAPSEATS_TILEPROPERTY, out val))
                    {
                        Rectangle thisTile = new Rectangle(i, j, 1, 1);

                        seatStringToTableRecs[val] = seatStringToTableRecs.TryGetValue(val, out var existingTileKey)
                            ? Rectangle.Union(thisTile, existingTileKey)
                            : thisTile;
                    }
                }
            }

            foreach (var pair in seatStringToTableRecs)
            {
                var splitValues = pair.Key.Split(' ');
                var seats = new List<Vector2>();

                for (int i = 0; i < splitValues.Length; i += 2)
                {
                    if (i + 1 >= splitValues.Length ||
                        !float.TryParse(splitValues[i], out float x) ||
                        !float.TryParse(splitValues[i + 1], out float y))
                    {
                        Log.Debug($"Invalid values in Cafe Map's seats at {pair.Value.X}, {pair.Value.Y}", LogLevel.Warn);
                        return;
                    }

                    Vector2 seatLocation = new(x, y);
                    seats.Add(seatLocation);
                }

                if (seats.Count > 0)
                {
                    MapTablesInCafeLocation.Add(pair.Value, seats);
                }
            }

            Log.Debug($"Updated map tables in the cafe: {string.Join(", ", MapTablesInCafeLocation.Select(pair => pair.Key.Center + " with " + pair.Value.Count + " seats"))}");
        }


        internal bool TryAddTable(Table table)
        {
            if (table.Seats.Count == 0)
                return false;

            table.Free();
            CurrentTables.Add(table);
            return true;
        }

        internal void RemoveTable(FurnitureTable table)
        {
            foreach (var seat in table.Seats)
            {
                FurnitureChair chair = (FurnitureChair)seat;
                chair.ActualChair.modData.Remove(ModKeys.MODDATA_CHAIRRESERVED);
                chair.ActualChair.modData.Remove(ModKeys.MODDATA_CHAIRTABLE);
            }

            table.ActualTable.modData.Remove(ModKeys.MODDATA_TABLERESERVED);
            if (!CurrentTables.Contains(table))
                Log.Debug("Trying to remove a table that isn't tracked", LogLevel.Warn);
            else
            {
                Log.Debug($"Table removed");
                CurrentTables.Remove(table);
            }
        }

        internal List<Table> GetFreeTables(int minimumSeats = 1)
        {
            return CurrentTables.OrderBy(_ => Game1.random.Next()).Where(t => !t.IsReserved && t.Seats.Count >= minimumSeats).ToList();
        }

        internal bool ChairIsReserved(Furniture chair)
        {
            return chair.modData.TryGetValue(ModKeys.MODDATA_CHAIRRESERVED, out var val) && val == "T";
        }

        internal void FreeAllTables()
            => CurrentTables.ForEach(t => t.Free());

        internal Table GetTableAt(GameLocation location, Vector2 position)
        {
            return CurrentTables.Where(t => t.CurrentLocation.Equals(location.Name)).FirstOrDefault(table => table.BoundingBox.Contains(position));
        }

        internal FurnitureTable TryAddFurnitureTable(Furniture table, GameLocation location)
        {
            FurnitureTable newTable = new FurnitureTable(table, location.Name);

            return TryAddTable(newTable)
                ? newTable : null;
        }

        internal void FarmerClickTable(Table table, Farmer who)
        {

        }

        internal void OnRenderedWorld(object sender, RenderedWorldEventArgs e)
        {
            // get list of reserved tables with center coords
            foreach (var table in CurrentTables)
            {
                if (table.IsReadyToOrder && Game1.currentLocation.Name.Equals(table.CurrentLocation))
                {
                    Vector2 offset = new Vector2(0,
                        (float)Math.Round(4f * Math.Sin(Game1.currentGameTime.TotalGameTime.TotalMilliseconds / 250.0)));

                    e.SpriteBatch.Draw(
                        Game1.mouseCursors,
                        Game1.GlobalToLocal(table.GetCenter() + new Vector2(-8, -64)) + offset,
                        new Rectangle(402, 495, 7, 16),
                        Color.Crimson,
                        0f,
                        new Vector2(1f, 4f),
                        4f,
                        SpriteEffects.None,
                        1f);
                }
            }
        }

        internal void OnFurnitureListChanged(object sender, FurnitureListChangedEventArgs e)
        {
            if (e.Location.Equals(CafeManager.Instance.CafeIndoors) || e.Location.Equals(Game1.getFarm()))
            {
                foreach (var f in e.Removed)
                {
                    if (Utility.IsChair(f))
                    {
                        FurnitureChair trackedChair = CurrentTables
                            .OfType<FurnitureTable>()
                            .SelectMany(t => t.Seats)
                            .OfType<FurnitureChair>()
                            .FirstOrDefault(seat => seat.Position == f.TileLocation && seat.Table.CurrentLocation.Equals(e.Location.Name));

                        if (trackedChair?.Table is not FurnitureTable table)
                            continue;

                        if (table.IsReserved)
                            Log.Debug("Removed a chair but the table was reserved");

                        table.RemoveChair(f);
                    }
                    else if (Utility.IsTable(f))
                    {
                        FurnitureTable trackedTable = Utility.IsTableTracked(f, e.Location);

                        if (trackedTable != null)
                        {
                            RemoveTable(trackedTable);
                        }
                    }
                }
                foreach (var f in e.Added)
                {
                    if (Utility.IsChair(f))
                    {
                        // Get position of table in front of the chair
                        Vector2 tablePos = f.TileLocation + (Utility.DirectionIntToDirectionVector(f.currentRotation.Value) * new Vector2(1, -1));

                        // Get table Furniture object
                        Furniture facingFurniture = e.Location.GetFurnitureAt(tablePos);

                        if (facingFurniture == null ||
                            !Utility.IsTable(facingFurniture) ||
                            facingFurniture
                                .GetBoundingBox()
                                .Intersects(f.boundingBox.Value)) // if chair was placed on top of the table
                        {
                            continue;
                        }

                        FurnitureTable table = Utility.IsTableTracked(facingFurniture, e.Location)
                                               ?? TryAddFurnitureTable(facingFurniture, e.Location);
                        table.AddChair(f);
                    }
                    else if (Utility.IsTable(f))
                    {
                        FurnitureTable table = Utility.IsTableTracked(f, e.Location);
                        if (table == null)
                            TryAddFurnitureTable(f, e.Location);
                    }
                }
            }
        }
    }
}
