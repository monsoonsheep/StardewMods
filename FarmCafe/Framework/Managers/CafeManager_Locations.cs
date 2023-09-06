using StardewValley;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using StardewValley.Pathfinding;
using static FarmCafe.Framework.Utility;
using StardewModdingAPI;
using xTile.Layers;
using xTile.Tiles;

namespace FarmCafe.Framework.Managers
{
    internal partial class CafeManager
    {
        internal static GameLocation GetCafeLocation()
        {
            return CafeLocations.FirstOrDefault(IsLocationCafe);
        }

        internal bool UpdateCafeLocation()
        {
            GameLocation foundCafe = Game1.getFarm().buildings
                .FirstOrDefault(IsBuildingCafe)
                ?.GetIndoors();

            GameLocation cachedCafeLocation = GetCafeLocation();

            if (foundCafe == null)
            {
                CafeLocations.Clear();
                CafeLocations.Add(Game1.getFarm());
                Logger.Log("Cafe is only on farm");
                return false;
            }

            if (cachedCafeLocation == null)
            {
                CafeLocations.Add(foundCafe);
            }
            else if (!foundCafe.Equals(cachedCafeLocation))
            {
                Tables.Where(t => t.CurrentLocation.Equals(cachedCafeLocation)).ToList().ForEach(t => t.CurrentLocation = foundCafe);
                CafeLocations.Remove(cachedCafeLocation);
                CafeLocations.Add(foundCafe);
            }

            if (CafeLocations.Count == 0)
            {
                CafeLocations.Add(Game1.getFarm());
            }
            else
            {
                GameLocation cafe = GetCafeLocation();
                if (cafe != null) 
                    PopulateMapTables(cafe);
            }

            return true;
        }


        internal void PopulateRoutesToCafe()
        {
            if (CafeLocations.Count == 0)
                return;

            foreach (string start in new[] { "BusStop", "Farm" })
            {
                GameLocation startLocation = GetLocationFromName(start);
                if (!RoutesToCafe.ContainsKey(start))
                    RoutesToCafe[start] = new List<LocationWarpRoute>();
                
                foreach (var cafe in CafeLocations)
                {
                    if (!RoutesToCafe.ContainsKey(cafe.NameOrUniqueName))
                        RoutesToCafe[cafe.NameOrUniqueName] = new List<LocationWarpRoute>();

                    RoutesToCafe[start].Add(FindLocationRouteToCafe(startLocation, cafe));
                    RoutesToCafe[cafe.NameOrUniqueName].Add(FindLocationRouteToCafe(cafe, startLocation));
                }
            }

            //RoutesToCafe.ForEach((route) => Logger.Log(string.Join(" - ", route)));
        }

        public LocationWarpRoute FindLocationRouteToCafe(GameLocation startLocation, GameLocation endLocation)
        {
            if (startLocation.Equals(endLocation))
            {
                return new LocationWarpRoute(new [] {startLocation.NameOrUniqueName}, null);
            }

            var frontier = new Queue<string>();
            frontier.Enqueue(startLocation.NameOrUniqueName);

            var cameFrom = new Dictionary<string, string>
            {
                [startLocation.NameOrUniqueName] = null
            };

            while (frontier.Count > 0)
            {
                string currentName = frontier.Dequeue();
                GameLocation current = GetLocationFromName(currentName);

                if (current == null)
                    continue;
                if (current.NameOrUniqueName == endLocation.NameOrUniqueName)
                    break;

                foreach (var name in current.warps.Select(warp => warp.TargetName)
                             .Where(name => !cameFrom.ContainsKey(name)))
                {
                    frontier.Enqueue(name);
                    cameFrom[name] = current.NameOrUniqueName;
                }

                foreach (var name in current.doors.Keys.Select(p => current.doors[p])
                             .Where(name => !cameFrom.ContainsKey(name)))
                {
                    frontier.Enqueue(name);
                    cameFrom[name] = current.NameOrUniqueName;
                }

               
                foreach (var building in current.buildings.Where(b => b.GetIndoors() != null))
                {
                    string name = building.GetIndoors().NameOrUniqueName;
                    if (cameFrom.ContainsKey(name)) continue;

                    frontier.Enqueue(name);
                    cameFrom[name] = current.NameOrUniqueName;
                }
            }

            List<string> path = new List<string>() { endLocation.NameOrUniqueName };
            string point = endLocation.NameOrUniqueName;
            while (true)
            {
                if (cameFrom.ContainsKey(point))
                {
                    path.Add(cameFrom[point]);
                    point = cameFrom[point];
                    if (point == startLocation.NameOrUniqueName) break;
                }
                else
                {
                    return null;
                }
            }

            path.Reverse();
            return new LocationWarpRoute(path.ToArray(), null);
        }

        internal string[] GetLocationRoute(GameLocation start, GameLocation end)
        {
            string[] route = WarpPathfindingCache.GetLocationRoute(start.Name, end.Name, NPC.male);

            if (route == null)
            {
                if (RoutesToCafe.TryGetValue(start.NameOrUniqueName, out var routes))
                {
                    foreach (LocationWarpRoute r in routes)
                    {
                        if (r.LocationNames[^1] == end.NameOrUniqueName)
                        {
                            route = r.LocationNames;
                            break;
                        }
                    }
                }
            }

            if (route == null)
            {
                // If an NPC wants to get out of the women's locker in the spa, this won't work. do something later.
                var routeToBus = WarpPathfindingCache.GetLocationRoute(start.NameOrUniqueName, "BusStop", NPC.male);
                if (routeToBus != null)
                {
                    var routeFromBusToEnd = GetLocationRoute(Game1.getLocationFromName("BusStop"), end);
                    if (routeFromBusToEnd != null)
                    {
                        return routeToBus.Take(routeToBus.Length - 1).Concat(routeFromBusToEnd).ToArray();
                    }
                }
            }
            // TODO reverse route (out of cafe)


            return route;
        }

        internal void PopulateMapTables(GameLocation location)
        {
            if (MapTablesInCafeLocation?.Count != 0)
                return;
            MapTablesInCafeLocation = new Dictionary<Rectangle, List<Vector2>>();
            Layer layer = location.Map.GetLayer("Back");

            Dictionary<string, Rectangle> seatStringToTableRecs = new();

            for (int i = 0; i < layer.LayerWidth; i++)
            {
                for (int j = 0; j < layer.LayerHeight; j++)
                {
                    Tile tile = layer.Tiles[i, j];
                    if (tile == null)
                        continue;

                    if (!tile.TileIndexProperties.TryGetValue("FarmCafeSeats", out string val) &&
                        !tile.Properties.TryGetValue("FarmCafeSeats", out val))
                        continue;

                    Rectangle thisTile = new Rectangle(i, j, 1, 1);

                    seatStringToTableRecs[val] = seatStringToTableRecs.TryGetValue(val, out var existingTileKey)
                        ? Rectangle.Union(thisTile, existingTileKey)
                        : thisTile;
                }
            }

            foreach (var pair in seatStringToTableRecs)
            {
                var splitValues = pair.Key.Split(' ');
                var seats = new List<Vector2>();

                for (int i = 0; i < splitValues.Length; i += 2)
                {
                    if (!float.TryParse(splitValues[i], out float x) ||
                        !float.TryParse(splitValues[i + 1], out float y))
                    {
                        Logger.Log($"Invalid values in Cafe Map's seats at {pair.Value.X}, {pair.Value.Y}", LogLevel.Warn);
                        continue;
                    }

                    Vector2 seatLocation = new(x, y);
                    seats.Add(seatLocation);
                }

                if (seats.Count > 0)
                {
                    MapTablesInCafeLocation.Add(pair.Value, seats);
                }

            }

            Logger.Log($"Updated map tables in the cafe: {string.Join(", ", MapTablesInCafeLocation.Select(pair => pair.Key.Center + " with " + pair.Value.Count + " seats"))}");
        }

    }
}
