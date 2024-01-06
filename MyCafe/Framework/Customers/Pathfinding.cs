using Microsoft.Xna.Framework;
using MyCafe.Framework.Managers;
using StardewValley;
using StardewValley.Objects;
using StardewValley.Pathfinding;
using System;
using System.Collections.Generic;
using System.Linq;
using xTile.Dimensions;

namespace MyCafe.Framework.Customers;

public static class Pathfinding
{
    
    public static bool PathTo(this NPC me, GameLocation targetLocation, Point targetTile, int finalFacingDirection = 0,
        PathFindController.endBehavior endBehavior = null)
    {
        me.controller = null;

        Stack<Point> path = null;
        try
        {
            path = PathfindFromLocationToLocation(me.currentLocation, me.TilePoint, targetLocation, targetTile, me);

            if (path == null || !path.Any())
            {
                throw new PathNotFoundException("Character couldn't find path.", me.TilePoint, targetTile, me.currentLocation.Name, targetLocation.Name, me);
            }
        }
        catch (PathNotFoundException e)
        {
            Log.Error("Error in PathTo:\n" + e);
            return false;
        }
        
        me.controller = new PathFindController(path, me.currentLocation, me, path.Last())
        {
            NPCSchedule = true,
            nonDestructivePathing = true,
            endBehaviorFunction = endBehavior,
            finalFacingDirection = finalFacingDirection
        };

        Log.Debug($"Pathing from {me.TilePoint} to {targetTile}");
        return true;
    }

    public static Stack<Point> PathfindFromLocationToLocation(GameLocation startingLocation, Point startTile,
        GameLocation targetLocation, Point targetTile, NPC character)
    {
        Point nextStartPosition = startTile;
        Stack<Point> path = new Stack<Point>();

        if (startingLocation.Name.Equals(targetLocation.Name))
        {
            return FindPath(nextStartPosition, targetTile, startingLocation, character);
        }

        string[] locationsRoute = WarpPathfindingCache.GetLocationRoute(startingLocation.Name, targetLocation.Name, character.Gender);
        if (locationsRoute == null)
            throw new Exception("Location route not found");

        for (int i = 0; i < locationsRoute.Length; i++)
        {
            GameLocation current = Utility.GetLocationFromName(locationsRoute[i]);
            if (i < locationsRoute.Length - 1)
            {
                Point target = (locationsRoute[i + 1] == CafeManager.Instance.CafeIndoors.Name)
                ? current.getWarpPointTo(CafeManager.Instance.CafeIndoors.uniqueName.Value)
                : current.getWarpPointTo(locationsRoute[i + 1]);

                if (target.Equals(Point.Zero))
                    throw new Exception("Error finding route to cafe. Couldn't find warp point");

                Stack<Point> nextPath = FindPath(nextStartPosition, target, current, character);
                if (nextPath == null)
                    return null;
                path = Utility.CombineStacks(path, nextPath);

                nextStartPosition = current.getWarpPointTarget(target);
                if (nextStartPosition == Point.Zero)
                {
                    GameLocation indoors = current.getBuildingAt(target.ToVector2()).GetIndoors();
                    if (indoors != null)
                    {
                        Warp w = indoors.warps.FirstOrDefault();
                        nextStartPosition = new Point(w.X, w.Y - 1);
                    }
                }
            }
            else
            {
                var nextPath = FindPath(nextStartPosition, targetTile, current, character);
                if (nextPath == null)
                    return null;
                path = Utility.CombineStacks(path, nextPath);
            }
        }

        return path;
    }

    public static Stack<Point> FindPath(Point startTile, Point targetTile, GameLocation location, Character character, int iterations = 30000)
    {

        Furniture furniture = location.GetFurnitureAt((targetTile).ToVector2());
        if (furniture != null || !location.isTilePassable(new Location(targetTile.X, targetTile.Y), Game1.viewport))
        {
            var directions = new List<sbyte[]>
            {
                new sbyte[] { 0, 1 }, // down
                new sbyte[] { -1, 0 }, // left
                new sbyte[] { 0, -1 }, // up
                new sbyte[] { 1, 0 }, // right
            };

            Stack<Point> shortestPath = null;
            foreach (var direction in directions)
            {
                Point newTile = targetTile + new Point(direction[0], direction[1]);

                if (location.GetFurnitureAt(newTile.ToVector2()) != null
                || !location.isTilePassable(new Location(newTile.X, newTile.Y), Game1.viewport))
                    continue;

                var p = FindPath(startTile, newTile, location, character, iterations: 1500);

                if (p == null || p.Count >= shortestPath?.Count)
                    continue;

                shortestPath = p;
            }
            return shortestPath;
        }
        else if (location is Farm or StardewValley.Locations.DecoratableLocation || location.GetContainingBuilding() != null)
        {
            // findPath checks for collisions with location.isCollidingPosition, passing in glider = false, pathfinding = true.
            // We do this on the farm because a. We put furniture there and b. Buildings on the farm aren't on the "Buildings" layer in the map for some reason,
            // so characters path and walk through the buildings if we do the other pathing method.
            return PathFindController.findPath(startTile, targetTile, PathFindController.isAtEndPoint, location, character, iterations);
        }
        else
        {
            // findPathForNPCSchedules doesn't look at collisions. It checks for tiles in the Buildings layer, pathing if 
            // there's a Passable or NPCPassable property, and not pathing through "NoPath" properties. It also 
            // checks for location.isTerrainFeatureAt(x, y)
            // We do this in NPC's homes because otherwise they can't go through their own doors
            return PathFindController.findPathForNPCSchedules(startTile, targetTile, location, 30000);
        }
    }
}


public class PathNotFoundException : Exception
{
    private readonly Point _fromTile;
    private readonly Point _toTile;
    private readonly string _fromLocation;
    private readonly string _toLocation;
    private readonly Character _forCharacter;

    /// <inheritdoc />
    public PathNotFoundException(string message, Point startPoint, Point targetPoint, string fromLocation, string toLocation, Character forCharacter) : base(message)
    {
        (_fromTile, _toTile, _fromLocation, _toLocation, _forCharacter) = (startPoint, targetPoint, fromLocation, toLocation, forCharacter);
    }

    public override string Message => $"From {_fromTile} in {_fromLocation} to {_toTile} in {_toLocation} for character {_forCharacter.Name}";
}
