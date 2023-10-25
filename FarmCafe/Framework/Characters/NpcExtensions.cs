using StardewModdingAPI;
using StardewValley.Objects;
using StardewValley;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using StardewValley.Pathfinding;
using xTile.Dimensions;
using static FarmCafe.Framework.Utility;
using static StardewValley.Pathfinding.PathFindController;
using System.Diagnostics.Metrics;
using System.Threading;
using Rectangle = Microsoft.Xna.Framework.Rectangle;

namespace FarmCafe.Framework.Characters
{
    internal static class NpcExtensions
    {
        internal static void HeadTowards(this NPC me, GameLocation targetLocation, Point targetTile, int finalFacingDirection = 0,
            Action endBehaviorFunction = null)
        {
            if (targetLocation == null)
            {
                Logger.Log("Invalid location in pathfinding..", LogLevel.Error);
                return;
            }
            me.controller = null;
            me.isCharging = false;

            Stack<Point> path = PathTo(me, me.currentLocation, me.TilePoint, targetLocation, targetTile);

            if (path == null || !path.Any())
            {
                Logger.Log("Character couldn't find path.", LogLevel.Warn);
                //GoHome();
                return;
            }

            me.controller = new PathFindController(path, me.currentLocation, me, path.Last())
            {
                NPCSchedule = true,
                nonDestructivePathing = true,
                endBehaviorFunction = (_, _) => endBehaviorFunction?.Invoke(),
                finalFacingDirection = finalFacingDirection
            };

            if (me.controller == null)
            {
                Logger.Log("Can't construct controller.", LogLevel.Error);
                //GoHome();
            }

            Logger.Log($"Pathing from {me.TilePoint} to {targetTile}");
        }


        internal static Stack<Point> PathTo(this NPC me, GameLocation startingLocation, Point startTile, GameLocation targetLocation,
            Point targetTile)
        {
            static Stack<Point> CombineStacks(Stack<Point> original, Stack<Point> toAdd)
            {
                if (toAdd == null)
                    return original;
            
                original = new Stack<Point>(original);
                while (original.Count > 0)
                    toAdd.Push(original.Pop());

                return toAdd;
            }

            Point locationStartPoint = startTile;
            if (startingLocation.Name.Equals(targetLocation.Name, StringComparison.Ordinal))
            {
                if ((IsLocationCafe(targetLocation) && !targetLocation.isTilePassable(new Location(targetTile.X, targetTile.Y), Game1.viewport)) 
                    || targetLocation.GetFurnitureAt(targetTile.ToVector2()) != null)
                {
                    return FindPathInLocation(me, locationStartPoint, targetTile, startingLocation, pathingToObject: true);
                }
                else
                {
                    return FindPathInLocation(me, locationStartPoint, targetTile, startingLocation);
                }
            }

            string[] locationsRoute = ModEntry.CafeManager.GetLocationRoute(startingLocation, targetLocation);
            if (locationsRoute == null)
                throw new Exception("Location route not found");

            Stack<Point> path = new Stack<Point>();
            for (int i = 0; i < locationsRoute.Length; i++)
            {
                GameLocation current = GetLocationFromName(locationsRoute[i]);
                if (i < locationsRoute.Length - 1)
                {
                    Point target = current.getWarpPointTo(locationsRoute[i + 1]);
                    if (target.Equals(Point.Zero))
                    {
                        throw new Exception("Error finding route to cafe. Couldn't find warp point");
                    }


                    Stack<Point> nextPath = FindPathInLocation(me, locationStartPoint, target, current);
                    if (nextPath == null)
                    {
                        return null;
                    }
                    path = CombineStacks(path, nextPath);

                    locationStartPoint = current.getWarpPointTarget(target);
                    if (locationStartPoint == Point.Zero)
                    {
                        var building = current.getBuildingAt(target.ToVector2());
                        if (building.GetIndoors() != null)
                        {
                            Warp w = building.GetIndoors().warps.FirstOrDefault();
                            locationStartPoint = new Point(w.X, w.Y - 1);
                        }
                    }
                }
                else
                {
                    Stack<Point> nextPathToAppend;
                    if ((IsLocationCafe(targetLocation) && !targetLocation.isTilePassable(new Location(targetTile.X, targetTile.Y), Game1.viewport)) 
                        || targetLocation.GetFurnitureAt(targetTile.ToVector2()) != null)
                    {
                        nextPathToAppend = FindPathInLocation(me, locationStartPoint, targetTile, current, pathingToObject: true);
                    }
                    else
                    {
                        nextPathToAppend = FindPathInLocation(me, locationStartPoint, targetTile, current);
                    }

                    path = CombineStacks(path, nextPathToAppend);
                }
            }

            return path;
        }

        internal static Stack<Point> FindPathInLocation(this NPC me, Point startTile, Point targetTile, GameLocation location, bool pathingToObject = false, int iterations = 30000)
        {
            if (pathingToObject)
            {
                return PathToObject(me, location, startTile, targetTile);
            }

            Stack<Point> path = null;

            if (location is Farm || IsLocationCafe(location))
            {
                // findPath checks for collisiong with location.isCollidingPosition, passing in glider = false, pathfinding = true.
                // We do this on the farm because a. We put furniture there and b. Buildings on the farm aren't on the "Buildings" layer in the map for some reason,
                // so characters path and walk through the buildings if we do the other pathing method.
                path = PathFindController.findPath(
                    startTile,
                    targetTile,
                    PathFindController.isAtEndPoint,
                    location,
                    me,
                    iterations);
            }
            // findPathForNPCSchedules doesn't look at collisions. It checks for tiles in the Buildings layer, pathing if 
            // there's a Passable or NPCPassable property, and not pathing through "NoPath" properties. It also 
            // checks for location.isTerrainFeatureAt(x, y)
            // We do this in NPC's homes because otherwise they can't go through their own doors
            path ??= PathFindController.findPathForNPCSchedules(startTile, targetTile, location, 30000);
            
            if (path == null)
            {
                Logger.Log($"Couldn't find path in {location.Name}");
            }
            return path;
        }

        internal static Stack<Point> PathToObject(this NPC me, GameLocation location, Point startTile, Point targetTile)
        {
            var directions = new List<sbyte[]>
            {
                new sbyte[] { 0, 1 }, // down
                new sbyte[] { -1, 0 }, // left
                new sbyte[] { 0, -1 }, // up
                new sbyte[] { 1, 0 }, // right
            };

            Furniture seat = location.GetFurnitureAt((targetTile).ToVector2());
            if (IsChair(seat) && !seat.Name.ToLower().Contains("stool"))
                directions.RemoveAt(seat.GetSittingDirection());

            MapSeat mapSeat = location.mapSeats.FirstOrDefault(s => s.OccupiesTile(targetTile.X, targetTile.Y));
            if (mapSeat != null)
                directions.RemoveAt(mapSeat.GetSittingDirection());
            
            Stack<Point> shortestPath = null;
            int shortestPathLength = int.MaxValue;

            foreach (var direction in directions)
            {
                Point newTile = (targetTile + new Point(direction[0], direction[1]));

                if (location.GetFurnitureAt(newTile.ToVector2()) != null || !location.isTilePassable(new Location(newTile.X, newTile.Y), Game1.viewport))
                    continue;

                var pathToAdjacentTile = FindPathInLocation(
                    me,
                    startTile,
                    newTile,
                    location,
                    pathingToObject: false,
                    iterations: 1500
                );

                if (pathToAdjacentTile == null || pathToAdjacentTile.Count >= shortestPathLength)
                    continue;

                shortestPath = pathToAdjacentTile;
                shortestPathLength = pathToAdjacentTile.Count;
            }

            if (shortestPath == null || shortestPath.Count == 0)
            {
                Logger.Log($"path to chair can't be found.");
            }

            return shortestPath;
        }

        public static bool IsAcceptingOfOrder(this NPC me)
        {
            return me is Customer; // later do regular NPC stuff
        }
    }
}
