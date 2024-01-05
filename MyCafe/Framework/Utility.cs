﻿using Microsoft.Xna.Framework;
using MyCafe.Framework.Customers;
using MyCafe.Framework.Managers;
using MyCafe.Framework.Objects;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.Objects;
using System;
using System.Collections.Generic;
using System.Linq;
using xTile.ObjectModel;
using xTile.Tiles;
using SObject = StardewValley.Object;

namespace MyCafe.Framework;

internal static class Utility
{
    internal static bool IsLocationCafe(GameLocation location)
    {
        return IsBuildingCafe(location?.GetContainingBuilding());
    }

    internal static bool IsBuildingCafe(Building building)
    {
        return building != null && building.GetData()?.CustomFields.TryGetValue("MonsoonSheep.MyCafe_IsCafeBuilding", out string val) == true && val.ToLower() == "true";
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
        if (table == null || location == null || TableManager.Instance == null)
            return null;
        return TableManager.Instance.CurrentTables
            .OfType<FurnitureTable>().FirstOrDefault(t => t.CurrentLocation.Equals(location.NameOrUniqueName) && t.Position == table.TileLocation);
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
                Log.Debug($"checking position {pos}");
                if (!location.isCollidingPosition(pos, Game1.viewport, character) && location.isCollidingWithWarp(pos, character) == null) // ???
                {
                    Log.Debug($"New position {startPos}");
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

    internal static Stack<Point> CombineStacks(Stack<Point> original, Stack<Point> toAdd)
    {
        if (toAdd == null)
            return original;

        original = new Stack<Point>(original);
        while (original.Count > 0)
            toAdd.Push(original.Pop());

        return toAdd;
    }

    internal static GameLocation GetLocationFromName(string name)
    {
        GameLocation loc = Game1.getLocationFromName(name);

        if (loc == null && CafeManager.Instance?.CafeIndoors != null
                        && (name.Equals(CafeManager.Instance.CafeIndoors.Name) || name.Equals(CafeManager.Instance.CafeIndoors.uniqueName.Value)))
        {
            return CafeManager.Instance.CafeIndoors;
        }

        return loc;
    }
}