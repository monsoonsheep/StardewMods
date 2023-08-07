using FarmCafe.Framework.Managers;
using FarmCafe.Locations;
using StardewModdingAPI;
using StardewValley.Locations;
using StardewValley.Objects;
using StardewValley;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using static FarmCafe.Framework.Utilities.Utility;

namespace FarmCafe.Framework.Characters
{
    internal static class NpcExtensions
    {
        
        internal static void HeadTowards(this NPC me, GameLocation targetLocation, Point targetTile, int finalFacingDirection = 0,
            Customer.BehaviorFunction endBehaviorFunction = null)
        {
            me.controller = null;
            me.isCharging = false;

            Stack<Point> path = PathTo(me, me.currentLocation, me.getTileLocationPoint(), targetLocation, targetTile);

            if (path == null || !path.Any())
            {
                Debug.Log("Customer couldn't find path.", LogLevel.Warn);
                //GoHome();
                return;
            }


            me.controller = new PathFindController(path, me.currentLocation, me, path.Last())
            {
                NPCSchedule = true,
                nonDestructivePathing = true,
                endBehaviorFunction = endBehaviorFunction != null ? (c, loc) => endBehaviorFunction() : null,
                finalFacingDirection = finalFacingDirection
            };

            if (me.controller == null)
            {
                Debug.Log("Can't construct controller.", LogLevel.Warn);
                //me.GoHome();
            }
        }


        internal static Stack<Point> PathTo(this NPC me, GameLocation startingLocation, Point startTile, GameLocation targetLocation,
            Point targetTile)
        {
            Stack<Point> path = new Stack<Point>();
            Point locationStartPoint = startTile;
            if (startingLocation.Name.Equals(targetLocation.Name, StringComparison.Ordinal))
                return FindPath(me, locationStartPoint, targetTile, startingLocation);

            List<string> locationsRoute = CafeManager.GetLocationRoute(startingLocation, targetLocation);

            if (locationsRoute == null)
            {
                locationsRoute = CafeManager.GetLocationRoute(targetLocation, startingLocation);
                if (locationsRoute == null)
                {
                    Debug.Log("Route to cafe not found!", LogLevel.Error);
                    return null;
                }
                else
                    locationsRoute.Reverse();
            }

            for (int i = 0; i < locationsRoute.Count; i++)
            {
                GameLocation current = GetLocationFromName(locationsRoute[i]);
                if (i < locationsRoute.Count - 1)
                {
                    Point target = current.getWarpPointTo(locationsRoute[i + 1]);
                    var cafeloc = GetLocationFromName(locationsRoute[i + 1]);
                    if (target == Point.Zero && cafeloc != null && cafeloc.Name.Contains("Cafe") &&
                        current is BuildableGameLocation buildableLocation)
                    {
                        var building = buildableLocation.buildings
                            .FirstOrDefault(b => b.indoors.Value is CafeLocation);
                        if (building == null || building.humanDoor == null)
                            throw new Exception("schedule pathing tried to find a warp point that doesn't exist.");
                        target = building.humanDoor.Value;
                    }

                    if (target.Equals(Point.Zero) || locationStartPoint.Equals(Point.Zero))
                        throw new Exception("schedule pathing tried to find a warp point that doesn't exist.");

                    path = CombineStacks(path, FindPath(me, locationStartPoint, target, current));
                    locationStartPoint = current.getWarpPointTarget(target);
                    if (locationStartPoint == Point.Zero)
                    {
                        var building = (current as BuildableGameLocation).getBuildingAt(target.ToVector2());
                        if (building != null && building.indoors.Value != null)
                        {
                            Warp w = building.indoors.Value.warps.FirstOrDefault();
                            locationStartPoint = new Point(w.X, w.Y - 1);
                        }
                    }
                }
                else
                {
                    path = CombineStacks(path, FindPath(me, locationStartPoint, targetTile, current));
                }
            }

            return path;
        }

        internal static Stack<Point> FindPath(this NPC me, Point startTile, Point targetTile, GameLocation location, int iterations = 600)
        {
            if (IsChair(location.GetFurnitureAt(targetTile.ToVector2())))
            {
                return PathToChair(me, location, startTile, targetTile, location.GetFurnitureAt(targetTile.ToVector2()));
            }
            if (location.Name.Equals("Farm"))
            {
                return PathFindController.FindPathOnFarm(startTile, targetTile, location, iterations);
            }

            return PathFindController.findPath(startTile, targetTile,
                PathFindController.isAtEndPoint, location, me, iterations);
        }

        internal static Stack<Point> PathToChair(this NPC me, GameLocation location, Point startTile, Point targetTile, Furniture chair)
        {
            var directions = new List<sbyte[]>
            {
                new sbyte[] { 0, -1 }, // up
                new sbyte[] { -1, 0 }, // left
                new sbyte[] { 0, 1 }, // down
                new sbyte[] { 1, 0 }, // right
            };

            if (!chair.Name.ToLower().Contains("stool"))
                directions.RemoveAt(chair.currentRotation.Value);

            Stack<Point> shortestPath = null;
            int shortestPathLength = 99999;

            foreach (var direction in directions)
            {
                Furniture obstructionChair =
                    location.GetFurnitureAt((targetTile + new Point(direction[0], direction[1])).ToVector2());
                if (IsChair(obstructionChair))
                    continue;

                var pathRightNextToChair = FindPath(
                    me,
                    startTile,
                    targetTile + new Point(direction[0], direction[1]),
                    location,
                    1500
                );

                if (pathRightNextToChair == null || pathRightNextToChair.Count >= shortestPathLength)
                    continue;

                shortestPath = pathRightNextToChair;
                shortestPathLength = pathRightNextToChair.Count;
            }

            if (shortestPath == null || shortestPath.Count == 0)
            {
                Debug.Log("path to chair can't be found");
            }

            return shortestPath;
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

    }
}
