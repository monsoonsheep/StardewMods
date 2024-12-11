using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewValley.Characters;
using StardewValley.Pathfinding;
using StardewValley.TerrainFeatures;
using xTile.Dimensions;
using xTile.Layers;

namespace StardewMods.SheepCore.Framework.Services;
public class Pathfinding
{
    public static Pathfinding Instance = null!;

    private readonly Dictionary<string, List<LocationWarpRoute>> LocationRoutes = [];

    internal readonly List<Point> NpcBarrierTiles = [];
    internal bool NpcBarrierRemoved;


    private readonly sbyte[,] Directions = new sbyte[4, 2]
    {
        { 0, -1 },
        { 1, 0 },
        { 0, 1 },
        { -1, 0 },
    };

    public Pathfinding()
        => Instance = this;

    internal void Initialize()
    {
        Mod.Events.GameLoop.SaveLoaded += this.OnSaveLoaded;
        Mod.Events.GameLoop.DayStarted += this.OnDayStarted;
        Mod.Harmony.Patch(
            AccessTools.Method(typeof(WarpPathfindingCache), nameof(WarpPathfindingCache.GetLocationRoute), [typeof(string), typeof(string), typeof(Gender)]),
            postfix: new HarmonyMethod(this.GetType(), nameof(After_GetLocationRoute))
            );
    }

    private static void After_GetLocationRoute(string startingLocation, string endingLocation, ref string[]? __result)
    {
        if (__result == null)
        {
            //__result = Instance.GetLocationRoute(startingLocation, endingLocation);
        }
    }

    private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
    {
        this.AddRoutesToFarm();
    }

    private void OnDayStarted(object? sender, DayStartedEventArgs e)
    {
        this.RemoveNpcBarrier();
    }

    public Stack<Point>? PathfindFromLocationToLocation(GameLocation startingLocation, Point startTile,
        GameLocation targetLocation, Point targetTile, NPC character)
    {
        Point nextStartPosition = startTile;
        Stack<Point> path = new Stack<Point>();

        if (startingLocation.Name.Equals(targetLocation.Name))
        {
            return this.FindPath(nextStartPosition, targetTile, startingLocation, character);
        }

        // Get location route
        string[]? locationsRoute = WarpPathfindingCache.GetLocationRoute(startingLocation.NameOrUniqueName, targetLocation.NameOrUniqueName, character.Gender)
            ?? this.GetLocationRoute(startingLocation.NameOrUniqueName, targetLocation.NameOrUniqueName);

        if (locationsRoute == null)
        {
            Log.Error("Macro-pathing location route not found");
            return null;
        }

        if (locationsRoute.Contains("Farm"))
        {
            this.RemoveNpcBarrier();
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

                Stack<Point>? nextPath = this.FindPath(nextStartPosition, target, current, character);

                if (nextPath == null)
                    return null;
                path = combineStacks(path, nextPath);

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
                Stack<Point>? nextPath = this.FindPath(nextStartPosition, targetTile, current, character);
                if (nextPath == null)
                    return null;
                path = combineStacks(path, nextPath);
            }
        }

        this.RestoreNpcBarrier();

        Stack<Point> combineStacks(Stack<Point> original, Stack<Point> toAdd)
        {
            original = new Stack<Point>(original);
            while (original.Count > 0)
                toAdd.Push(original.Pop());
            return toAdd;
        }

        return path.Count == 0 ? null : path;
    }

    public Stack<Point>? FindPath(Point startTile, Point targetTile, GameLocation location, Character? character, int iterations = 30000)
    {
        if (location is Farm || location.ParentBuilding?.GetParentLocation() is Farm)
        {
            Log.Trace($"Pathing {character?.Name} through {location.Name} with custom pathing (avoiding furniture) ({startTile} to {targetTile})");
            return this.PathfindImpl(location, startTile, targetTile, character, iterations);

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
        return PathFindController.findPathForNPCSchedules(startTile, targetTile, location, 30000, character);
    }

    private Stack<Point>? PathfindImpl(GameLocation location, Point startPoint, Point endPoint, Character? character, int limit = 5000)
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
                int neighborX = currentNode.x + this.Directions[i, 0];
                int neighborY = currentNode.y + this.Directions[i, 1];
                int neighborHash = PathNode.ComputeHash(neighborX, neighborY);

                if (visited.Contains(neighborHash))
                    continue;

                PathNode neighbor = new(neighborX, neighborY, currentNode);
                neighbor.g = (byte)(currentNode.g + 1);

                if (((neighborX == endPoint.X && neighborY == endPoint.Y) || (neighborX >= 0 && neighborY >= 0 && neighborX < layerWidth && neighborY < layerHeight))
                    && !location.isCollidingPosition(new Microsoft.Xna.Framework.Rectangle(neighbor.x * 64 + 1, neighbor.y * 64 + 1, 62, 62), Game1.viewport, false, 0, glider: false, character, pathfinding: true))
                {
                    int neighborScore = neighbor.g + this.GetPreferenceValueForTerrainType(location, neighbor.x, neighbor.y) +
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

    /// <summary>
    /// This works on the farm to prefer flooring tiles over ground, and dirt over grass.
    /// </summary>
    private int GetPreferenceValueForTerrainType(GameLocation location, int x, int y)
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

    public Point GetEntryPointIntoLocation(GameLocation location, GameLocation fromLocation)
    {
        Point warpOutPoint = location.getWarpPointTo(fromLocation.Name);
        Point warpedOutTile = location.getWarpPointTarget(warpOutPoint);

        Point entryPoint = Point.Zero;

        // Find the warp back into the first location
        foreach (Warp w in fromLocation.warps)
        {
            if (Math.Abs(w.X - warpedOutTile.X) <= 2 && Math.Abs(w.Y - warpedOutTile.Y) <= 2)
            {
                entryPoint = new Point(w.TargetX, w.TargetY);
                break;
            }
        }
        foreach (KeyValuePair<Point, string> door in fromLocation.doors.Pairs)
        {
            if ((door.Value == location.Name || door.Value == location.NameOrUniqueName)
                && Math.Abs(door.Key.X - warpedOutTile.X) <= 2 && Math.Abs(door.Key.Y - warpedOutTile.Y) <= 2)
            {
                entryPoint = fromLocation.getWarpPointTarget(door.Key);
                break;
            }
        }
        return entryPoint;
    }

    private void AddRoute(List<string> route)
    {
        if (!this.LocationRoutes.ContainsKey(route[0]))
            this.LocationRoutes[route[0]] = new List<LocationWarpRoute>();

        this.LocationRoutes[route[0]].Add(new LocationWarpRoute(route.ToArray(), null));
    }

    public string[]? GetLocationRoute(string startingLocation, string endingLocation)
    {
        if (this.LocationRoutes.TryGetValue(startingLocation, out var routes))
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
    public void AddRoutesToFarm()
    {
        Log.Trace("Adding routes to farm");

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

            this.AddRoute(route);
            this.AddRoute(reverseRoute);
        }
    }

    /// <summary>
    /// Use <see cref="WarpPathfindingCache"/>.AddRoute to add routes from all locations to the given location and vice versa
    /// </summary>
    public void AddRoutesToBuildingInFarm(GameLocation indoorLocation)
    {
        Log.Trace($"Adding routes to location {indoorLocation.NameOrUniqueName}");
        if (this.LocationRoutes.Count(pair => pair.Value.Any(route => route.LocationNames[^1].Equals(indoorLocation.NameOrUniqueName))) > 10)
        {
            // Already added routes for this target location
            return;
        }

        foreach (GameLocation location in Game1.locations)
        {
            List<string>? toFarm = (WarpPathfindingCache.GetLocationRoute(location.Name, "Farm", Gender.Undefined) ?? this.GetLocationRoute(location.Name, "Farm"))?.ToList();
            if (toFarm is not { Count: > 1 })
            {
                continue;
            }

            List<string> route = toFarm.Concat([indoorLocation.NameOrUniqueName]).ToList();

            var reverseRoute = new List<string>(route);
            reverseRoute.Reverse();

            this.AddRoute(route);
            this.AddRoute(reverseRoute);
        }
    }

    public void RemoveNpcBarrier()
    {
        if (!this.NpcBarrierRemoved)
        {
            Layer layer = Game1.getFarm().Map.GetLayer("Back");
            foreach (Point point in this.NpcBarrierTiles)
            {
                layer.Tiles[point.X, point.Y]?.Properties.Remove("NPCBarrier");
            }

            this.NpcBarrierRemoved = true;
        }
    }

    public void RestoreNpcBarrier()
    {
        if (this.NpcBarrierRemoved)
        {
            Layer layer = Game1.getFarm().Map.GetLayer("Back");

            foreach (Point point in this.NpcBarrierTiles)
            {
                layer.Tiles[point.X, point.Y]?.Properties.Remove("NPCBarrier");
            }

            this.NpcBarrierRemoved = false;
        }
    }
}
