using StardewValley.Locations;
using StardewValley.Objects;
using StardewValley;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using FarmCafe.Framework.Characters;
using Microsoft.Xna.Framework;
using SolidFoundations.Framework.Models.ContentPack;
using StardewModdingAPI;
using StardewValley.Characters;
using xTile.Layers;
using xTile.ObjectModel;
using xTile.Tiles;

namespace FarmCafe.Locations
{
    public class CafeLocation : DecoratableLocation
    {
        internal Dictionary<Rectangle, List<Vector2>> MapTables;

        public CafeLocation()
        {
            MapTables = new Dictionary<Rectangle, List<Vector2>>();
        }

        internal void PopulateMapTables()
        {
            if (MapTables?.Count != 0)
                return;
            MapTables = new Dictionary<Rectangle, List<Vector2>>();
            Layer layer = Map.GetLayer("Back");

            Dictionary<string, Rectangle> seatStringToTableRecs = new();

            for (int i = 0; i < layer.LayerWidth; i++)
            {
                for (int j = 0; j < layer.LayerHeight; j++)
                {
                    Tile tile = layer.Tiles[i, j];
                    if (tile == null)
                        continue;

                    if (!tile.TileIndexProperties.TryGetValue("FarmCafeSeats", out PropertyValue val) &&
                        !tile.Properties.TryGetValue("FarmCafeSeats", out val))
                        continue;

                    Rectangle thisTile = new Rectangle(i, j, 1, 1);

                    seatStringToTableRecs[val] = seatStringToTableRecs.TryGetValue(val, out var existingTileKey)
                        ? Rectangle.Union(thisTile, existingTileKey)
                        : thisTile;
                }
            }

            foreach (var pair in seatStringToTableRecs)
            {
                var splitValues = pair.Key.Split(' ');
                var seats = new List<Vector2>();

                for (int i = 0; i < splitValues.Length; i += 2)
                {
                    if (!float.TryParse(splitValues[i], out float x) ||
                        !float.TryParse(splitValues[i + 1], out float y))
                    {
                        Logger.Log($"Invalid values in Cafe Map's seats at {pair.Value.X}, {pair.Value.Y}", LogLevel.Warn);
                        continue;
                    }

                    Vector2 seatLocation = new(x, y);
                    seats.Add(seatLocation);
                }

                if (seats.Count > 0)
                {
                    MapTables.Add(pair.Value, seats);
                }

            }

            Logger.Log($"Updated map tables in the cafe: {string.Join(", ", MapTables.Select(pair => pair.Key.Center + " with " + pair.Value.Count + " seats"))}");
        }

        // Probably not needed because of the postfix?
        //public override void cleanupBeforeSave()
        //{
        //    for (var i = characters.Count - 1; i >= 0; i--)
        //    {
        //        if (characters[i] is Customer) 
        //        {
        //            characters.RemoveAt(i);
        //        }
        //    }
        //}

        public override void DayUpdate(int dayOfMonth)
        {
            base.DayUpdate(dayOfMonth);
            foreach (var item in furniture)
            {
                item.updateDrawPosition();
            }
        }
    }
}
