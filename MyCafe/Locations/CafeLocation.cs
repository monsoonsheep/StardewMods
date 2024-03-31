using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Xml.Serialization;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using xTile;
using xTile.Layers;
using xTile.Tiles;

// ReSharper disable once CheckNamespace

namespace StardewValley.Locations;

[XmlType("Mods_MonsoonSheep_MyCafe_CafeLocation")]
public class CafeLocation : GameLocation
{
    private readonly Dictionary<Rectangle, List<Vector2>> MapTables = new();

    private const string TableMapProperty = "MonsoonSheep.MyCafe_Table";

    public CafeLocation() { }

    public CafeLocation(string mapPath, string name) : base(mapPath, name) { }

    public Dictionary<Rectangle, List<Vector2>> GetMapTables()
    {
        return this.MapTables;
    }

    [SuppressMessage("ReSharper", "ParameterHidesMember", Justification = "Game method")]
    public override void OnMapLoad(Map map)
    {
        base.OnMapLoad(map);
        this.PopulateMapTables();
    }

    private void PopulateMapTables()
    {
        this.MapTables.Clear();
        Layer layer = this.Map.GetLayer("Back");

        Dictionary<string, Rectangle> tableRectangles = new();

        for (int i = 0; i < layer.LayerWidth; i++)
        {
            for (int j = 0; j < layer.LayerHeight; j++)
            {
                Tile tile = layer.Tiles[i, j];
                if (tile == null)
                    continue;

                if (tile.TileIndexProperties.TryGetValue(TableMapProperty, out var val)
                    || tile.Properties.TryGetValue(TableMapProperty, out val))
                {
                    Rectangle thisTile = new Rectangle(i, j, 1, 1);

                    tableRectangles[val] = tableRectangles.TryGetValue(val, out var existingTileKey)
                        ? Rectangle.Union(thisTile, existingTileKey)
                        : thisTile;
                }
            }
        }

        foreach (var rect in tableRectangles)
        {
            string[] splitValues = rect.Key.Split(' ');
            var seats = new List<Vector2>();

            for (int i = 0; i < splitValues.Length; i += 2)
            {
                if (i + 1 >= splitValues.Length || !float.TryParse(splitValues[i], out float x) || !float.TryParse(splitValues[i + 1], out float y))
                {
                    Console.WriteLine($"Invalid values in Cafe Map's seats at {rect.Value.X}, {rect.Value.Y}", LogLevel.Warn);
                    return;
                }

                Vector2 seatLocation = new(x, y);
                seats.Add(seatLocation);
            }

            if (seats.Count > 0)
            {
                this.MapTables.Add(rect.Value, seats);
            }
        }
    }
}
