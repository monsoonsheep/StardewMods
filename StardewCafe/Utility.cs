using VisitorFramework.Framework.Managers;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Objects;
using System;
using System.Collections.Generic;
using System.Linq;
using StardewCafe.Framework;
using StardewCafe.Framework.Objects;
using StardewValley.Buildings;
using StardewValley.Pathfinding;
using VisitorFramework.Framework.Visitors;
using xTile.ObjectModel;
using xTile.Tiles;
using SObject = StardewValley.Object;

namespace StardewCafe
{
    internal class Utility
    {
        internal static bool IsLocationCafe(GameLocation location)
        {
            return IsBuildingCafe(location?.GetContainingBuilding());
        }

        internal static bool IsBuildingCafe(Building building)
        {
            return building != null && building.modData.ContainsKey("IsCafeBuilding");

        }

        internal static Item GetItem(string id)
        {
            return new SObject(id, 1).getOne();
        }

        internal static bool IsChair(Furniture furniture)
        {
            return furniture != null && furniture.furniture_type.Value is 0 or 1 or 2;
        }

        internal static bool IsTable(Furniture furniture)
        {
            return furniture.furniture_type.Value == 11;
        }

        internal static FurnitureTable IsTableTracked(Furniture table, GameLocation location)
        {
            if (table == null || location == null)
                return null;
            return CafeManager.Tables
                .OfType<FurnitureTable>().FirstOrDefault(t => t.CurrentLocation.Equals(location) && t.Position == table.TileLocation);
        }

        internal static Vector2 DirectionIntToDirectionVector(int direction)
        {
            return direction switch
            {
                0 => new Vector2(0, -1),
                1 => new Vector2(1, 0),
                2 => new Vector2(0, 1),
                3 => new Vector2(-1, 0),
                _ => new Vector2(0, 0)
            };
        }

        internal static int DirectionIntFromVectors(Vector2 startTile, Vector2 facingTile)
        {
            int xDist = (int)Math.Abs(startTile.X - facingTile.X);
            int yDist = (int)Math.Abs(startTile.Y - facingTile.Y);

            if (yDist == 0 || xDist > yDist)
            {
                return startTile.X > facingTile.X ? 3 : 1;
            }
            else if (xDist == 0 || yDist > xDist)
            {
                return startTile.Y > facingTile.Y ? 0 : 2;
            }

            return -1;
        }

        internal static IEnumerable<Point> AdjacentTilesCollision(Point startPos, GameLocation location, Visitor character, int reach = 1)
        {
            for (int i = startPos.X - reach; i < startPos.X + reach; i++)
            {
                for (int j = startPos.Y - reach; i < startPos.Y + reach; j++)
                {
                    if (i == 0 && j == 0) continue;

                    var pos = new Rectangle((startPos.X + i) * 64, (startPos.Y + j) * 64, 62, 62);
                    Logger.Log($"checking position {pos}");
                    if (!location.isCollidingPosition(pos, Game1.viewport, character) && location.isCollidingWithWarp(pos, character) == null) // ???
                    {
                        Logger.Log($"New position {startPos}");
                        yield return new Point(startPos.X + i, startPos.Y + j);
                    }

                }
            }
        }

        internal static IEnumerable<Point> AdjacentTiles(Point startPos, int reach = 1)
        {
            for (int i = startPos.X - reach; i <= startPos.X + reach; i++)
            {
                for (int j = startPos.Y - reach; i <= startPos.Y + reach; j++)
                {
                    if (i == startPos.X && j == startPos.Y) continue;
                    yield return startPos + new Point(i, j);
                }
            }
        }

        internal static string GetTileProperties(Tile tile)
        {
            return tile == null ? "there's no tile" : tile.Properties.ToList().Concat(tile.TileIndexProperties.ToList() ?? new List<KeyValuePair<string, PropertyValue>>()).Aggregate("", (currentTile, property) => currentTile + $"{property.Key}: {property.Value}, ");
        }

        internal static GameLocation GetLocationFromName(string name)
        {
            return Game1.getLocationFromName(name) ?? CafeManager.CafeLocations.FirstOrDefault(a => a.Name == name);
        }
    }

    public class ItemEqualityComparer : IEqualityComparer<Item>
    {
        public bool Equals(Item x, Item y)
        {
            return x != null && y != null && x.ParentSheetIndex == y.ParentSheetIndex;
        }

        public int GetHashCode(Item obj) => obj != null ? obj.ParentSheetIndex * 900 : -1;
    }
}
