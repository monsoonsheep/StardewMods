using FarmCafe.Framework.Customers;
using FarmCafe.Framework.Managers;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Objects;
using System;
using System.Collections.Generic;
using System.Linq;
using FarmCafe.Framework.Characters;
using xTile.Tiles;

namespace FarmCafe.Framework.Utilities
{
	internal static class Utility
	{
        internal static GameLocation GetLocationFromName(string name)
        {
            var l = Game1.getLocationFromName(name);
            if (l == null)
            {
                l = CafeManager.CafeLocations.Where(a => a.Name == name).FirstOrDefault();
            }
            return l;
        }

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

		internal static Point DirectionIntToDirectionPoint(int direction)
		{
			return direction switch
			{
				0 => new Point(0, -1),
				1 => new Point(1, 0),
				2 => new Point(0, 1),
				3 => new Point(-1, 0),
				_ => new Point(0, 0)
			};
		}

		internal static int DirectionIntFromPoints(Point startTile, Point facingTile)
		{
			if (startTile.X != facingTile.X)
			{
				return startTile.X > facingTile.X ? 3 : 1;
			}
			else if (startTile.Y != facingTile.Y)
			{
				return startTile.Y > facingTile.Y ? 0 : 2;
			}

			return -1;
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

		internal static Point GetRandomAdjacentTile(Point fromPoint, GameLocation location, Customer character = null)
		{
			for (int i = -1; i <= 1; i++)
			{
				for (int j = -1; j <= 1; j++)
				{
					if (i == 0 && j == 0) continue;

					var pos = new Rectangle((fromPoint.X + i) * 64, (fromPoint.Y + j) * 64, 62, 62);

					if (!location.isCollidingPosition(pos, Game1.viewport, character) && location.isCollidingWithWarp(pos, character) == null) // ???
					{
						return new Point(fromPoint.X + i, fromPoint.Y + j);
					}

				}
			}

			return Point.Zero;
		}

		internal static IEnumerable<Point> AdjacentTilesCollision(Point startPos, GameLocation location, Customer character, int reach = 1)
		{
			for (int i = startPos.X - reach; i < startPos.X + reach; i++)
			{
				for (int j = startPos.Y - reach; i < startPos.Y + reach; j++)
				{
					if (i == 0 && j == 0) continue;

					var pos = new Rectangle((startPos.X + i) * 64, (startPos.Y + j) * 64, 62, 62);
					Debug.Log($"checking position {pos}");
					if (!location.isCollidingPosition(pos, Game1.viewport, character) && location.isCollidingWithWarp(pos, character) == null) // ???
					{
						Debug.Log($"New position {startPos}");
						yield return new Point(startPos.X + i, startPos.Y + j);
					}

				}
			}
		}

		internal static Point GetAdjacentTileCollision(Point startPos, GameLocation location, Customer character, int reach = 1)
		{
			return AdjacentTilesCollision(startPos, location, character, reach).FirstOrDefault();
		}

		internal static IEnumerable<Point> AdjacentTiles(Point startPos, int reach = 1)
		{
			for (int i = startPos.X - reach; i < startPos.X + reach; i++)
			{
				for (int j = startPos.Y - reach; i < startPos.Y + reach; j++)
				{
					if (i == startPos.X && j == startPos.Y) continue;
					yield return startPos + new Point(i, j);
				}
			}
		}
        internal static string GetTileProperties(Tile tile)
        {
            return tile == null ? "" : tile.Properties.Concat(tile.TileIndexProperties).Aggregate("", (currentTile, property) => currentTile + $"{property.Key}: {property.Value}, ");
        }

        private static void DebugRepositionCustomer(int x, int y)
        {
            if (CustomerManager.CurrentCustomers.Any())
            {
                Customer c = CustomerManager.CurrentCustomers.First();
                c.Position = new Vector2(x, y);
            }

        }
    }
}
