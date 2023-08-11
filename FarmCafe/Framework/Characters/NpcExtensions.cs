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
            Action endBehaviorFunction = null)
        {
            if (targetLocation == null)
            {
                Debug.Log("Invalid location in pathfinding..", LogLevel.Error);
                return;
            }
            me.controller = null;
            me.isCharging = false;

            Stack<Point> path = PathTo(me, me.currentLocation, me.getTileLocationPoint(), targetLocation, targetTile);

            if (path == null || !path.Any())
            {
                Debug.Log("Character couldn't find path.", LogLevel.Warn);
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
                Debug.Log("Can't construct controller.", LogLevel.Error);
                //me.GoHome();
            }
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
                return FindPath(me, locationStartPoint, targetTile, startingLocation);

            List<string> locationsRoute = FarmCafe.cafeManager.GetLocationRoute(startingLocation, targetLocation);
            if (locationsRoute == null)
                throw new Exception("Location route not found");

            Stack<Point> path = new Stack<Point>();
            for (int i = 0; i < locationsRoute.Count; i++)
            {
                GameLocation current = FarmCafe.cafeManager.GetLocationFromName(locationsRoute[i]);
                if (i < locationsRoute.Count - 1)
                {
                    Point target = current.getWarpPointTo(locationsRoute[i + 1]);

                    // If the next location in the route is a Cafe Indoors (getWarpPointTo doesn't find SF indoors)
                    if (target == Point.Zero && FarmCafe.cafeManager.GetLocationFromName(locationsRoute[i + 1]) is CafeLocation &&
                        current is BuildableGameLocation buildableLocation)
                    {
                        var building = buildableLocation.buildings
                            .FirstOrDefault(b => b.indoors.Value is CafeLocation);
                        if (building == null)
                            throw new Exception("schedule pathing tried to find a warp point that doesn't exist.");

                        target = building.humanDoor.Value;
                    }

                    if (target.Equals(Point.Zero) || locationStartPoint.Equals(Point.Zero))
                        throw new Exception("Error finding route to cafe. Couldn't find warp point");

                    path = CombineStacks(path, FindPath(me, locationStartPoint, target, current));
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
                    path = CombineStacks(path, FindPath(me, locationStartPoint, targetTile, current));
                }
            }

            return path;
        }

        internal static Stack<Point> FindPath(this NPC me, Point startTile, Point targetTile, GameLocation location, int iterations = 600)
        {
            if (IsChair(location.GetFurnitureAt(targetTile.ToVector2())))
            {
                return PathToObject(me, location, startTile, targetTile);
            }
            if (location.Name.Equals("Farm"))
            {
                return PathFindController.FindPathOnFarm(startTile, targetTile, location, iterations);
            }

            return PathFindController.findPath(
                startTile, 
                targetTile,
                PathFindController.isAtEndPoint, 
                location, 
                me, 
                iterations);
        }

        internal static Stack<Point> PathToObject(this NPC me, GameLocation location, Point startTile, Point targetTile)
        {
            var directions = new List<sbyte[]>
            {
                new sbyte[] { 0, -1 }, // up
                new sbyte[] { -1, 0 }, // left
                new sbyte[] { 0, 1 }, // down
                new sbyte[] { 1, 0 }, // right
            };

            Furniture furniture = location.GetFurnitureAt((targetTile).ToVector2());
            if (IsChair(furniture) && !furniture.Name.ToLower().Contains("stool"))
                directions.RemoveAt(furniture.currentRotation.Value);
            

            Stack<Point> shortestPath = null;
            int shortestPathLength = int.MaxValue;

            foreach (var direction in directions)
            {
                if (location.GetFurnitureAt((targetTile + new Point(direction[0], direction[1])).ToVector2()) != null)
                    continue;

                var pathToAdjacentTile = FindPath(
                    me,
                    startTile,
                    targetTile + new Point(direction[0], direction[1]),
                    location,
                    1500
                );

                if (pathToAdjacentTile == null || pathToAdjacentTile.Count >= shortestPathLength)
                    continue;

                shortestPath = pathToAdjacentTile;
                shortestPathLength = pathToAdjacentTile.Count;
            }

            if (shortestPath == null || shortestPath.Count == 0)
            {
                Debug.Log($"path to chair can't be found.");
            }

            return shortestPath;
        }
    }
}
