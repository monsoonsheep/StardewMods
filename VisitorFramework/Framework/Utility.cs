#region Usings
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Objects;
using System;
using System.Collections.Generic;
using System.Linq;
using VisitorFramework.Framework.Visitors;
using StardewValley.Buildings;
using StardewValley.Pathfinding;
using xTile.ObjectModel;
using xTile.Tiles;
using SObject = StardewValley.Object;
#endregion

namespace VisitorFramework.Framework
{
    internal class Utility
    {
        /// <summary>
        /// Input a direction (0,1,2,3) and get a Vector that's a step in that direction from (0, 0)
        /// </summary>
        /// <param name="direction"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Input two vectors and return a direction int (0,1,2,3) that's in the direction from the starting vector to the facing vector
        /// </summary>
        /// <returns></returns>
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

        /// <summary>
        /// Return an enumerable of free tiles adjacent to the input tile in the given location. Collisions are checked based on the input <see cref="Character"/>
        /// </summary>
        /// <returns></returns>
        internal static List<Point> AdjacentTilesCollision(Point startPos, GameLocation location, int reach = 1)
        {
            List<Point> points = new List<Point>();
            for (int i = startPos.X - reach; i < startPos.X + reach; i++)
            {
                for (int j = startPos.Y - reach; j < startPos.Y + reach; j++)
                {
                    if (i == 0 && j == 0) continue;

                    Rectangle pos = new Rectangle((i) * 64, (j) * 64, 62, 62);

                    if (!location.isCollidingPosition(pos, Game1.viewport, false, 0, false, null, pathfinding: true, projectile: false, ignoreCharacterRequirement: true) && location.isCollidingWithWarp(pos, null) == null) // ???
                    {
                        points.Add(new Point(i, j));
                    }
                }
            }

            return points;
        }

        /// <summary>
        /// Return an enumerable of tiles around the given tile within the given radius, without checking for collisions.
        /// </summary>
        /// <param name="startPos">Starting tile</param>
        /// <param name="reach">The radius</param>
        /// <returns></returns>
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

        /// <summary>
        /// Get a string containing a list of properterties on a tile in the Map
        /// </summary>
        /// <param name="tile"></param>
        /// <returns></returns>
        internal static string GetTileProperties(Tile tile)
        {
            return tile == null ? "there's no tile" : tile.Properties.ToList().Concat(tile.TileIndexProperties.ToList() ?? new List<KeyValuePair<string, PropertyValue>>()).Aggregate("", (currentTile, property) => currentTile + $"{property.Key}: {property.Value}, ");
        }

        /// <summary>
        /// Returns true if the given tile in the given location is colliding with something (An object or terrain feature or building)
        /// </summary>
        /// <returns></returns>
        internal static bool IsTileCollidingInLocation(GameLocation location, Point tilePoint)
        {
            Rectangle tileRec = new Rectangle((tilePoint.X) * 64, (tilePoint.Y) * 64, 62, 62);
            return (location.isCollidingPosition(tileRec, Game1.viewport, false, 0, false, null) && location.isCollidingWithWarp(tileRec, null) == null);
        }

        internal static GameLocation GetLocationFromName(string name)
        {
            return Game1.getLocationFromName(name);
        }
    }
}
