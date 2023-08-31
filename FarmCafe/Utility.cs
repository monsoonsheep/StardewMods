using FarmCafe.Framework.Managers;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Objects;
using System;
using System.Collections.Generic;
using System.Linq;
using FarmCafe.Framework.Characters;
using FarmCafe.Framework.Objects;
using xTile.Tiles;

namespace FarmCafe
{
    internal class Utility
    {
        internal static bool IsChair(Furniture furniture)
        {
            return furniture != null && furniture.furniture_type.Value is 0 or 1 or 2;
        }

        internal static bool IsTable(Furniture furniture)
        {
            return furniture.furniture_type.Value == 11;
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

        internal static IEnumerable<Point> AdjacentTilesCollision(Point startPos, GameLocation location, Customer character, int reach = 1)
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
            return tile == null ? "there's no tile" : tile.Properties.Concat(tile.TileIndexProperties).Aggregate("", (currentTile, property) => currentTile + $"{property.Key}: {property.Value}, ");
        }

        internal static GameLocation GetLocationFromName(string name)
        {
            return Game1.getLocationFromName(name) ?? ModEntry.CafeLocations.FirstOrDefault(a => a.Name == name);
        }

        internal static FurnitureTable IsTableTracked(Furniture table, GameLocation location)
        {
            return ModEntry.Tables
                .OfType<FurnitureTable>().FirstOrDefault(t => t.CurrentLocation.Equals(location) && t.Position == table.TileLocation);
        }

        internal static List<locationPathDescription> GetLocationRouteFromSchedule(NPC npc)
        {
            // time, location name, steps to get there
            List<locationPathDescription> route = new();
            Dictionary<int, SchedulePathDescription> schedule = npc.getSchedule(Game1.dayOfMonth);

            GameLocation currentLoc = Game1.getLocationFromName(npc.DefaultMap);

            var ordered = schedule.OrderBy(pair => pair.Key).ToList();
            foreach (var pathDescription in ordered)
            {
                int steps = 0;

                while (pathDescription.Value.route.Count > 0)
                {
                    steps++;
                    Point cursor = pathDescription.Value.route.Pop();

                    Warp w = currentLoc.isCollidingWithWarpOrDoor(new Rectangle(cursor.X * 64, cursor.Y * 64, 62, 62));
                    if (w != null)
                        currentLoc = Game1.getLocationFromName(w.TargetName);
                }

                route.Add(new locationPathDescription(pathDescription.Key, currentLoc.Name, steps));
            }

            return route;
        }

    }
}
