using StardewModdingAPI;
using StardewValley.Locations;
using StardewValley.Objects;
using StardewValley;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using xTile.Dimensions;
using static FarmCafe.Framework.Utility;

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

            Stack<Point> path = PathTo(me, me.currentLocation, me.getTileLocationPoint(), targetLocation, targetTile);

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

            Logger.Log($"Pathing from {me.getTileLocationPoint()} to {targetTile}");
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
                if ((targetLocation is CafeLocation && !targetLocation.isTilePassable(new Location(targetTile.X, targetTile.Y), Game1.viewport)) 
                    || targetLocation.GetFurnitureAt(targetTile.ToVector2()) != null)
                {
                    return FindPathInLocation(me, locationStartPoint, targetTile, startingLocation, pathingToObject: true);
                }
                else
                {
                    return FindPathInLocation(me, locationStartPoint, targetTile, startingLocation);
                }
            }

            List<string> locationsRoute = ModEntry.CafeManager.GetLocationRoute(startingLocation, targetLocation);
            if (locationsRoute == null)
                throw new Exception("Location route not found");

            Stack<Point> path = new Stack<Point>();
            for (int i = 0; i < locationsRoute.Count; i++)
            {
                GameLocation current = GetLocationFromName(locationsRoute[i]);
                if (i < locationsRoute.Count - 1)
                {
                    Point target = current.getWarpPointTo(locationsRoute[i + 1]);

                    // If the next location in the route is a Cafe Indoors (getWarpPointTo doesn't find SF indoors)
                    if (target == Point.Zero && GetLocationFromName(locationsRoute[i + 1]) is CafeLocation &&
                        current is BuildableGameLocation buildableLocation)
                    {
                        var building = buildableLocation.buildings
                            .FirstOrDefault(b => b.indoors.Value is CafeLocation);
                        if (building == null)
                            throw new Exception("schedule pathing tried to find a warp point that doesn't exist.");
                        target = building.humanDoor.Value;
                    }

                    if (target.Equals(Point.Zero)) // || locationStartPoint.Equals(Point.Zero))
                        throw new Exception("Error finding route to cafe. Couldn't find warp point");


                    Stack<Point> nextPath = FindPathInLocation(me, locationStartPoint, target, current);
                    if (nextPath == null)
                    {
                        return null;
                    }
                    path = CombineStacks(path, nextPath);

                    locationStartPoint = current.getWarpPointTarget(target);
                    if (locationStartPoint == Point.Zero)
                    {
                        var building = (current as BuildableGameLocation)?.getBuildingAt(target.ToVector2());
                        if (building?.indoors.Value != null)
                        {
                            Warp w = building.indoors.Value.warps.FirstOrDefault();
                            locationStartPoint = new Point(w.X, w.Y - 1);
                        }
                    }
                }
                else
                {
                    Stack<Point> nextPathToAppend;
                    if ((targetLocation is CafeLocation && !targetLocation.isTilePassable(new Location(targetTile.X, targetTile.Y), Game1.viewport)) 
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

            if (location is Farm)
            {
                path = PathFindController.FindPathOnFarm(startTile, targetTile, location, iterations);
            }
            else if (!me.Name.Contains("Customer"))
            {
                path = PathFindController.findPathForNPCSchedules(startTile, targetTile, location, 30000);
            }
            

            path ??= PathFindController.findPath(
                startTile,
                targetTile,
                PathFindController.isAtEndPoint,
                location,
                me,
                iterations);

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
                directions.RemoveAt(seat.GetSittingDirection()); // 3 is left

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
