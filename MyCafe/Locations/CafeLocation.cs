using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Objects;
using xTile.Layers;
using xTile.Tiles;

// ReSharper disable once CheckNamespace
namespace MyCafe.Locations;
[XmlType("Mods_MonsoonSheep_MyCafe_CafeLocation")]
public class CafeLocation : GameLocation
{
    private readonly Dictionary<Rectangle, List<Vector2>> _mapTablesInCafeLocation = new();

    private const string TABLE_MAP_PROPERTY = "MonsoonSheep.MyCafe_Table";

    public CafeLocation()
    {

    }

    public CafeLocation(string mapPath, string name) : base(mapPath, name) { }

    public Dictionary<Rectangle, List<Vector2>> GetMapTables()
    {
        return _mapTablesInCafeLocation;
    }

    public void PopulateMapTables()
    {
        if (_mapTablesInCafeLocation is { Count: > 0 })
            return;

        _mapTablesInCafeLocation.Clear();
        Layer layer = Map.GetLayer("Buildings");

        Dictionary<string, Rectangle> seatStringToTableRecs = new();

        for (int i = 0; i < layer.LayerWidth; i++)
        {
            for (int j = 0; j < layer.LayerHeight; j++)
            {
                Tile tile = layer.Tiles[i, j];
                if (tile == null)
                    continue;

                if (tile.TileIndexProperties.TryGetValue(TABLE_MAP_PROPERTY, out var val)
                    || tile.Properties.TryGetValue(TABLE_MAP_PROPERTY, out val))
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
                if (i + 1 >= splitValues.Length || !float.TryParse(splitValues[i], out float x) || !float.TryParse(splitValues[i + 1], out float y))
                {
                    Console.WriteLine($"Invalid values in Cafe Map's seats at {pair.Value.X}, {pair.Value.Y}", LogLevel.Warn);
                    return;
                }

                Vector2 seatLocation = new(x, y);
                seats.Add(seatLocation);
            }

            if (seats.Count > 0)
            {
                _mapTablesInCafeLocation.Add(pair.Value, seats);
            }
        }
    }
}
