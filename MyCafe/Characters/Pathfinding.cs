using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Microsoft.Xna.Framework;
using MyCafe.Game;
using StardewValley;
using StardewValley.Pathfinding;
using StardewValley.TerrainFeatures;
using xTile.Layers;
using xTile.Tiles;
using Location = xTile.Dimensions.Location;
using Rectangle = Microsoft.Xna.Framework.Rectangle;

namespace MyCafe.Characters;

public static class Pathfinding
{
    private static readonly Dictionary<string, List<LocationWarpRoute>> LocationRoutes = [];

    internal static readonly List<Point> NpcBarrierTiles = [];
    internal static bool NpcBarrierRemoved;

    private static readonly sbyte[,] Directions = new sbyte[4 ,2]
    {
        { 0, -1 },
        { 1, 0 },
        { 0, 1 },
        { -1, 0 },
    };

    /// <summary>
    /// Pathfinds the character to the given location and position, and sets its <see cref="Character.controller"/>, disrupting their current schedule behavior
    /// </summary>
    /// <exception cref="PathNotFoundException">Thrown when pathfinding failed due to either macro level location routes not found or tile collisions in a location</exception>
    public static bool PathTo(this NPC me, GameLocation targetLocation, Point targetTile, int finalFacingDirection = 0,
        PathFindController.endBehavior? endBehavior = null)
    {
        Stack<Point>? path;

        RemoveNpcBarrier();
        PathFindController? originalController = me.controller;
        me.controller = null;
        try
        {
            path = PathfindFromLocationToLocation(me.currentLocation, me.TilePoint, targetLocation, targetTile, me);
        }
        finally
        {
            RestoreNpcBarrier();
        }

        if (path == null || path.Count == 0)
        {
            me.controller = originalController;
            throw new PathNotFoundException($"Error finding route to cafe. Couldn't find warp point for {me.Name}", me.TilePoint, targetTile,
                me.currentLocation.Name, targetLocation.Name, me);
        }

        me.temporaryController = null;
        me.Halt();
        AccessTools.Method(typeof(NPC), "prepareToDisembarkOnNewSchedulePath").Invoke(me, null);
        me.controller = new PathFindController(path, me.currentLocation, me, path.Last())
        {
            NPCSchedule = true,
            endBehaviorFunction = endBehavior,
            finalFacingDirection = finalFacingDirection
        };
        
        if (me.get_IsSittingDown().Value)
        {
            me.JumpOutOfChair();
        }
        Log.Trace($"NPC is at {me.TilePoint}");
        Log.Trace($"Path: ({(string.Join("-", path.Select(point => $"({point.X},{point.Y}) ")))}");
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
        string[]? locationsRoute = WarpPathfindingCache.GetLocationRoute(startingLocation.NameOrUniqueName, targetLocation.NameOrUniqueName, character.Gender)
            ?? GetLocationRoute(startingLocation.NameOrUniqueName, targetLocation.NameOrUniqueName);

        if (locationsRoute == null)
        {
            Log.Error("Macro-pathing location route not found");
            return null;
        }

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

    private static Stack<Point>? FindPath(Point startTile, Point targetTile, GameLocation location, Character? character, int iterations = 30000)
    {
        if (ModUtility.IsChairHere(location, startTile))
        {
            Log.Trace($"Pathing {character?.Name} through {location.Name} accounting for starting chair ({startTile} to {targetTile})");
            return FindShortestPathFromChair(location, startTile, targetTile, character);
        }

        if (ModUtility.IsChairHere(location, targetTile))
        {
            Log.Trace($"Pathing {character?.Name} through {location.Name} accounting for ending chair ({startTile} to {targetTile})");
            return FindShortestPathToChair(location, startTile, targetTile, character);
        }

        if (location is Farm || location.GetContainingBuilding() != null || location.Equals(Mod.Cafe.Signboard?.Location))
        {
            Log.Trace($"Pathing {character?.Name} through {location.Name} with custom pathing (avoiding furniture) ({startTile} to {targetTile})");
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
        Log.Trace($"Pathing {character?.Name} through {location.Name} with default schedule pathing ({startTile} to {targetTile})");
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

            if (p != null && !(p.Count >= shortestPath?.Count))
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

    private static void AddRoute(List<string> route)
    {
        if (!LocationRoutes.ContainsKey(route[0]))
            LocationRoutes[route[0]] = new List<LocationWarpRoute>();
        
        LocationRoutes[route[0]].Add(new LocationWarpRoute(route.ToArray(), null));
    }

    private static string[]? GetLocationRoute(string startingLocation, string endingLocation)
    {
        if (LocationRoutes.TryGetValue(startingLocation, out var routes))
        {
            foreach (LocationWarpRoute route in routes)
            {
                if (route.LocationNames[^1] == endingLocation)
                {
                    Gender? onlyGender = route.OnlyGender;
                    if (!onlyGender.HasValue)
                    {
                        return route.LocationNames;
                    }
                }
            }
        }
        return null;
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
    internal static void AddRoutesToBuildingInFarm(GameLocation indoorLocation)
    {
        if (LocationRoutes.Count(pair => pair.Value.Any(route => route.LocationNames[^1].Equals(indoorLocation.NameOrUniqueName))) > 10)
        {
            // Already added routes for this target location
            return;
        }

        foreach (GameLocation location in Game1.locations)
        {
            List<string>? toFarm = (WarpPathfindingCache.GetLocationRoute(location.Name, "Farm", Gender.Undefined) ?? GetLocationRoute(location.Name, "Farm"))?.ToList();
            if (toFarm is not { Count: > 1 })
            {
                continue;
            }

            List<string> route = toFarm.Concat([indoorLocation.NameOrUniqueName]).ToList();

            var reverseRoute = new List<string>(route);
            reverseRoute.Reverse();

            AddRoute(route);
            AddRoute(reverseRoute);
        }
    }

    private static void RemoveNpcBarrier()
    {
        if (!NpcBarrierRemoved)
        {
            Layer layer = Game1.getFarm().Map.GetLayer("Back");
            foreach (Point point in NpcBarrierTiles)
            {
                layer.Tiles[point.X, point.Y]?.Properties.Remove("NPCBarrier");
            }

            NpcBarrierRemoved = true;
        }
    }

    private static void RestoreNpcBarrier()
    {
        if (NpcBarrierRemoved)
        {
            Layer layer = Game1.getFarm().Map.GetLayer("Back");

            foreach (Point point in NpcBarrierTiles)
            {
                layer.Tiles[point.X, point.Y]?.Properties.Remove("NPCBarrier");
            }

            NpcBarrierRemoved = false;
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
