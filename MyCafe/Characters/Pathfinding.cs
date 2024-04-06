using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Pathfinding;
using StardewValley.TerrainFeatures;
using Location = xTile.Dimensions.Location;
using Rectangle = Microsoft.Xna.Framework.Rectangle;

namespace MyCafe.Characters;

public static class Pathfinding
{
    private static readonly Action<List<string>> AddRoute = (route) => AddRouteMethod?.Invoke(null, [route, null]);
    private static readonly MethodInfo AddRouteMethod = AccessTools.Method(typeof(WarpPathfindingCache), "AddRoute", [typeof(List<string>), typeof(Gender?)]);

    private static readonly sbyte[,] Directions = new sbyte[4 ,2]
    {
        { 0, -1 },
        { 1, 0 },
        { 0, 1 },
        { -1, 0 },
    };

    public static bool PathTo(this NPC me, GameLocation targetLocation, Point targetTile, int finalFacingDirection = 0,
        PathFindController.endBehavior? endBehavior = null)
    {
        Stack<Point>? path = PathfindFromLocationToLocation(me.currentLocation, me.TilePoint, targetLocation, targetTile, me);
        if (path == null || path.Count == 0)
        {
            Log.Error("Couldn't pathfind NPC");
            throw new PathNotFoundException($"Error finding route to cafe. Couldn't find warp point for {me.Name}", me.TilePoint, targetTile,
                me.currentLocation.Name, targetLocation.Name, me);
        }

        me.temporaryController = null;
        AccessTools.Method(typeof(NPC), "prepareToDisembarkOnNewSchedulePath").Invoke(me, null);

        me.controller = new PathFindController(path, me.currentLocation, me, path.Last())
        {
            NPCSchedule = true,
            endBehaviorFunction = endBehavior,
            finalFacingDirection = finalFacingDirection
        };

        return true;
    }

    public static Stack<Point>? PathfindFromLocationToLocation(GameLocation startingLocation, Point startTile,
        GameLocation targetLocation, Point targetTile, NPC character)
    {
        Point nextStartPosition = startTile;
        Stack<Point> path = new Stack<Point>();

        if (startingLocation.Name.Equals(targetLocation.Name))
        {
            return FindPath(nextStartPosition, targetTile, startingLocation, character);
        }

        // Get location route
        string[] locationsRoute = WarpPathfindingCache.GetLocationRoute(startingLocation.Name, targetLocation.Name, character.Gender)
            ?? throw new PathNotFoundException($"Location route not found for {character.Name}", startTile, targetTile, startingLocation.Name, targetLocation.Name, character);

        for (int i = 0; i < locationsRoute.Length; i++)
        {
            GameLocation? current = Game1.getLocationFromName(locationsRoute[i]);
            if (current == null)
                return null;
            
            if (i < locationsRoute.Length - 1)
            {
                Point target = current.getWarpPointTo(locationsRoute[i + 1]);

                if (target.Equals(Point.Zero))
                    return null;

                Stack<Point>? nextPath = FindPath(nextStartPosition, target, current, character);

                if (nextPath == null)
                    return null;
                path = CombineStacks(path, nextPath);

                nextStartPosition = current.getWarpPointTarget(target);
                if (nextStartPosition == Point.Zero)
                {
                    GameLocation? indoors = current.getBuildingAt(target.ToVector2()).GetIndoors();
                    if (indoors?.warps.FirstOrDefault() is { } warp)
                        nextStartPosition = new Point(warp.X, warp.Y - 1);
                }
            }
            else
            {
                Stack<Point>? nextPath = FindPath(nextStartPosition, targetTile, current, character);
                if (nextPath == null)
                    return null;
                path = CombineStacks(path, nextPath);
            }
        }

        static Stack<Point> CombineStacks(Stack<Point> original, Stack<Point> toAdd)
        {
            original = new Stack<Point>(original);
            while (original.Count > 0)
                toAdd.Push(original.Pop());
            return toAdd;
        }

        if (path.Count == 0)
            return null;
        
        return path;
    }

    public static Stack<Point>? FindPath(Point startTile, Point targetTile, GameLocation location, Character? character, int iterations = 30000)
    {
        if (location.GetFurnitureAt(targetTile.ToVector2()) != null
            || !location.isTilePassable(new Location(targetTile.X, targetTile.Y), Game1.viewport))
        {
            return FindShortestPathToChair(location, startTile, targetTile, character);
        }

        if (location.GetFurnitureAt(startTile.ToVector2()) != null
            || !location.isTilePassable(new Location(startTile.X, startTile.Y), Game1.viewport))
        {
            return FindShortestPathFromChair(location, startTile, targetTile, character);
        }

        if (location is Farm || location.GetContainingBuilding() != null)
        {
            // Experimental
            return Pathfinding.PathfindImpl(location, startTile, targetTile, character, iterations);

            // findPath checks for collisions with location.isCollidingPosition, passing in glider = false, pathfinding = true.
            // We do this on the farm because a. We put furniture there and b. Buildings on the farm aren't on the "Buildings" layer in the map for some reason,
            // so characters path and walk through the buildings if we do the other pathing method.
            //return PathFindController.findPath(startTile, targetTile, PathFindController.isAtEndPoint, location, character, iterations);
        }
        
        // findPathForNPCSchedules doesn't look at collisions. It checks for tiles in the Buildings layer, pathing if 
        // there's a Passable or NPCPassable property, and not pathing through "NoPath" properties. It also 
        // checks for location.isTerrainFeatureAt(x, y)
        // We do this in NPC's homes because otherwise they can't go through their own doors
        return PathFindController.findPathForNPCSchedules(startTile, targetTile, location, 30000);
    }

    private static Stack<Point>? PathfindImpl(GameLocation location, Point startPoint, Point endPoint, Character? character, int limit = 5000)
    {
        PriorityQueue frontier = new();
        HashSet<int> visited = new();

        frontier.Clear();
        visited.Clear();

        PathNode previousNode = new PathNode(startPoint.X, startPoint.Y, 0, null);
        frontier.Enqueue(previousNode, Math.Abs(endPoint.X - startPoint.X) + Math.Abs(endPoint.Y - startPoint.Y));

        int count = 0;
        int layerWidth = location.map.Layers[0].LayerWidth;
        int layerHeight = location.map.Layers[0].LayerHeight;

        while (!frontier.IsEmpty() && count < limit)
        {
            PathNode currentNode = frontier.Dequeue();
            if (currentNode.x == endPoint.X && currentNode.y == endPoint.Y)
                return PathFindController.reconstructPath(currentNode);
            
            visited.Add(currentNode.id);

            for (int i = 0; i < 4; i++)
            {
                int neighborX = currentNode.x + Directions[i, 0];
                int neighborY = currentNode.y + Directions[i, 1];
                int neighborHash = PathNode.ComputeHash(neighborX, neighborY);

                if (visited.Contains(neighborHash))
                    continue;

                PathNode neighbor = new(neighborX, neighborY, currentNode);
                neighbor.g = (byte) (currentNode.g + 1);

                if (((neighborX == endPoint.X && neighborY == endPoint.Y) || (neighborX >= 0 && neighborY >= 0 && neighborX < layerWidth && neighborY < layerHeight))
                    && !location.isCollidingPosition(new Rectangle(neighbor.x * 64 + 1, neighbor.y * 64 + 1, 62, 62), Game1.viewport, false, 0, glider: false, character, pathfinding: true))
                {
                    int neighborScore = neighbor.g + GetPreferenceValueForTerrainType(location, neighbor.x, neighbor.y) +
                                        (Math.Abs(endPoint.X - neighborX) + Math.Abs(endPoint.Y - neighborY))
                                        + ((neighbor.x == currentNode.x && neighbor.x == previousNode.x) || (neighbor.y == currentNode.y && neighbor.y == previousNode.y) ? -2 : 0);

                    if (!frontier.Contains(neighbor, neighborScore))
                        frontier.Enqueue(neighbor, neighborScore);
                }
            }

            previousNode = currentNode;
            count++;
        }

        return null;
    }

    private static Stack<Point>? FindShortestPathToChair(GameLocation location, Point startTile, Point targetTile, Character? character)
    {
        List<sbyte[]> directions =
        [
            [0, 1], // down
            [-1, 0], // left
            [0, -1], // up
            [1, 0] // right
        ];

        Stack<Point>? shortestPath = null;
        foreach (sbyte[]? direction in directions)
        {
            Point newTile = targetTile + new Point(direction[0], direction[1]);

            if (location.GetFurnitureAt(newTile.ToVector2()) != null
                || !location.isTilePassable(new Location(newTile.X, newTile.Y), Game1.viewport))
                continue;

            Stack<Point>? p = FindPath(startTile, newTile, location, character, iterations: 1500);

            if (p == null || p.Count >= shortestPath?.Count)
                continue;

            shortestPath = p;
        }
        return shortestPath;
    }

    private static Stack<Point>? FindShortestPathFromChair(GameLocation location, Point startTile, Point targetTile, Character? character)
    {
        List<sbyte[]> directions =
        [
            [0, 1], // down
            [-1, 0], // left
            [0, -1], // up
            [1, 0] // right
        ];

        Stack<Point>? shortestPath = null;
        foreach (sbyte[]? direction in directions)
        {
            Point newTile = startTile + new Point(direction[0], direction[1]);

            if (location.getBuildingAt(newTile.ToVector2()) != null ||
                location.getObjectAtTile(newTile.X, newTile.Y) != null ||
                //location.GetFurnitureAt(newTile.ToVector2()) != null ||
                !location.isTilePassable(new Location(newTile.X, newTile.Y), Game1.viewport))
                continue;

            Stack<Point>? p = FindPath(newTile, targetTile, location, character, iterations: 1500);

            if (p == null || p.Count >= shortestPath?.Count)
                continue;

            shortestPath = p;
        }
        return shortestPath;
    }

    /// <summary>
    /// This works on the farm to prefer flooring tiles over ground, and dirt over grass.
    /// </summary>
    private static int GetPreferenceValueForTerrainType(GameLocation location, int x, int y)
    {
        string str = location.doesTileHaveProperty(x, y, "Type", "Back");
        int value = 0;
        if (str != null)
        {
            switch (str.ToLower())
            {
                case "stone":
                    value = -7;
                    break;
                case "wood":
                    value = -4;
                    break;
                case "dirt":
                    value = -2;
                    break;
                case "grass":
                    value = -1;
                    break;
            }
        }

        if (location.terrainFeatures.TryGetValue(new Vector2(x, y), out var terrainFeature) && terrainFeature is Flooring)
            value -= 7;
        
        return value;
    }

    /// <summary>
    /// Use <see cref="WarpPathfindingCache"/>.AddRoute to add routes from all locations to the farm and vice versa
    /// </summary>
    internal static void AddRoutesToFarm()
    {
        foreach (GameLocation gameLocation in Game1.locations)
        {
            List<string>? route = gameLocation.Name.Equals("BusStop")
                ? ["BusStop", "Farm"]
                : WarpPathfindingCache.GetLocationRoute(gameLocation.Name, "BusStop", Gender.Undefined)?.Concat(["Farm"]).ToList();

            if (route is not { Count: > 1 })
            {
                continue;
            }

            var reverseRoute = new List<string>(route);
            reverseRoute.Reverse();

            AddRoute(route);
            AddRoute(reverseRoute);
        }
    }

    /// <summary>
    /// Use <see cref="WarpPathfindingCache"/>.AddRoute to add routes from all locations to the given location and vice versa
    /// </summary>
    internal static void AddRoutesToBuildingInFarm(GameLocation building)
    {
        foreach (GameLocation location in Game1.locations)
        {
            List<string>? toFarm = WarpPathfindingCache.GetLocationRoute(location.Name, "Farm", Gender.Undefined)?.ToList();
            if (toFarm is not { Count: > 1 })
            {
                continue;
            }

            List<string>? route = toFarm.Concat([building.Name]).ToList();

            var reverseRoute = new List<string>(route);
            reverseRoute.Reverse();

            AddRoute(route);
            AddRoute(reverseRoute);
        }
    }
}

public class PathNotFoundException : Exception
{
    private readonly Point FromTile;
    private readonly Point ToTile;
    private readonly string FromLocation;
    private readonly string ToLocation;
    private readonly Character ForCharacter;

    /// <inheritdoc />
    public PathNotFoundException(string message, Point startPoint, Point targetPoint, string fromLocation, string toLocation, Character forCharacter) : base(message)
    {
        (this.FromTile, this.ToTile, this.FromLocation, this.ToLocation, this.ForCharacter) = (startPoint, targetPoint, fromLocation, toLocation, forCharacter);
    }

    public override string Message => $"From {this.FromTile} in {this.FromLocation} to {this.ToTile} in {this.ToLocation} for character {this.ForCharacter.Name}";
}
