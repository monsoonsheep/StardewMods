using StardewModdingAPI;
using StardewValley.Characters;
using StardewValley.Objects;
using StardewValley.Pathfinding;
using StardewValley;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using MyCafe.Framework.Managers;
using xTile.Dimensions;

namespace MyCafe.Framework.Customers
{
    public static class NpcExtensions
    {
        public static void HeadTowards(this NPC me, GameLocation targetLocation, Point targetTile, int finalFacingDirection = 0,
            PathFindController.endBehavior endBehavior = null)
        {
            me.controller = null;

            Stack<Point> path = PathfindFromLocationToLocation(me, me.currentLocation, me.TilePoint, targetLocation, targetTile);

            if (path == null || !path.Any())
            {
                Log.Warn("Character couldn't find path.");
                return;
            }

            me.controller = new PathFindController(path, me.currentLocation, me, path.Last())
            {
                NPCSchedule = true,
                nonDestructivePathing = true,
                endBehaviorFunction = endBehavior,
                finalFacingDirection = finalFacingDirection
            };

            if (me.controller == null)
            {
                Log.Error("Can't construct controller.");
            }

            Log.Debug($"Pathing from {me.TilePoint} to {targetTile}");
        }


        public static Stack<Point> PathfindFromLocationToLocation(this NPC me, GameLocation startingLocation, Point startTile, 
            GameLocation targetLocation, Point targetTile)
        {
            Point locationStartPoint = startTile;

            if (startingLocation.Name.Equals(targetLocation.Name))
            {
                return FindPathInLocation(me, locationStartPoint, targetTile, startingLocation);
            }

            // string[] locationsRoute = CafeManager.Instance.GetLocationRoute(startingLocation, targetLocation);
            string[] locationsRoute = WarpPathfindingCache.GetLocationRoute(startingLocation.Name, targetLocation.Name, me.Gender);
            if (locationsRoute == null)
                throw new Exception("Location route not found");

            Stack<Point> path = new Stack<Point>();
            for (int i = 0; i < locationsRoute.Length; i++)
            {
                GameLocation current = Utility.GetLocationFromName(locationsRoute[i]);
                if (i < locationsRoute.Length - 1)
                {
                    Point target = current.getWarpPointTo(locationsRoute[i + 1]);
                    if (target.Equals(Point.Zero))
                    {
                        if (locationsRoute[i + 1] == CafeManager.Instance.CafeIndoors.Name) {
                            target = current.getWarpPointTo(CafeManager.Instance.CafeIndoors.uniqueName.Value);
                        }
                        if (target.Equals(Point.Zero))
                            throw new Exception("Error finding route to cafe. Couldn't find warp point");
                    }

                    Stack<Point> nextPath = FindPathInLocation(me, locationStartPoint, target, current);
                    if (nextPath == null)
                    {
                        return null;
                    }
                    path = Utility.CombineStacks(path, nextPath);

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
                    var nextPathToAppend = FindPathInLocation(me, locationStartPoint, targetTile, current);
                    path = Utility.CombineStacks(path, nextPathToAppend);
                }
            }

            return path;
        }

        public static Stack<Point> FindPathInLocation(this NPC me, Point startTile, Point targetTile, GameLocation location, int iterations = 30000)
        {
            Stack<Point> path = null;

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

                if (furniture != null && !furniture.Name.ToLower().Contains("stool"))
                    directions.RemoveAt(furniture.GetSittingDirection());

                MapSeat mapSeat = location.mapSeats.FirstOrDefault(s => s.OccupiesTile(targetTile.X, targetTile.Y));
                if (mapSeat != null)
                    directions.RemoveAt(mapSeat.GetSittingDirection());
            
                int shortestPathLength = int.MaxValue;

                foreach (var direction in directions)
                {
                    Point newTile = targetTile + new Point(direction[0], direction[1]);

                    if (location.GetFurnitureAt(newTile.ToVector2()) != null || !location.isTilePassable(new Location(newTile.X, newTile.Y), Game1.viewport))
                        continue;

                    var pathToAdjacentTile = FindPathInLocation(
                        me,
                        startTile,
                        newTile,
                        location,
                        iterations: 1500
                    );

                    if (pathToAdjacentTile == null || pathToAdjacentTile.Count >= shortestPathLength)
                        continue;

                    path = pathToAdjacentTile;
                    shortestPathLength = pathToAdjacentTile.Count;
                }

                if (path == null || path.Count == 0)
                {
                    Log.Debug($"path to chair can't be found.");
                }
            }
            else if (location is Farm || location.GetContainingBuilding() != null)
            {
                // findPath checks for collisions with location.isCollidingPosition, passing in glider = false, pathfinding = true.
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
            else
            {
                // findPathForNPCSchedules doesn't look at collisions. It checks for tiles in the Buildings layer, pathing if 
                // there's a Passable or NPCPassable property, and not pathing through "NoPath" properties. It also 
                // checks for location.isTerrainFeatureAt(x, y)
                // We do this in NPC's homes because otherwise they can't go through their own doors
                path = PathFindController.findPathForNPCSchedules(startTile, targetTile, location, 30000);
            }

            if (path == null)
                Log.Error($"Couldn't find path in {location.Name}");
            
            return path;
        }
    }
}
