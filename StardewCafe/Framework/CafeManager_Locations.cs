using StardewValley;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using StardewValley.Pathfinding;
using StardewModdingAPI;
using xTile.Layers;
using xTile.ObjectModel;
using xTile.Tiles;

namespace StardewCafe.Framework
{
    internal partial class CafeManager
    {
        internal static GameLocation GetCafeLocation()
        {
            return CafeLocations.FirstOrDefault(Utility.IsLocationCafe);
        }

        internal static bool UpdateCafeLocation()
        {
            GameLocation foundCafe = Game1.getFarm().buildings
                .FirstOrDefault(Utility.IsBuildingCafe)
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

        internal static void PopulateRoutesToCafe()
        {
            if (CafeLocations.Count == 0)
                return;

            foreach (string start in new[] { "BusStop", "Farm" })
            {
                GameLocation startLocation = Utility.GetLocationFromName(start);
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

        public static LocationWarpRoute FindLocationRouteToCafe(GameLocation startLocation, GameLocation endLocation)
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
                GameLocation current = Utility.GetLocationFromName(currentName);

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

        internal static void PopulateMapTables(GameLocation location)
        {
            if (MapTablesInCafeLocation?.Count != 0)
                return;
            MapTablesInCafeLocation = new Dictionary<Rectangle, List<Vector2>>();
            Layer layer = location.Map.GetLayer("Buildings");

            Dictionary<string, Rectangle> seatStringToTableRecs = new();

            for (int i = 0; i < layer.LayerWidth; i++)
            {
                for (int j = 0; j < layer.LayerHeight; j++)
                {
                    Tile tile = layer.Tiles[i, j];
                    if (tile == null)
                        continue;

                    if (!tile.TileIndexProperties.TryGetValue(ModKeys.MAPSEATS_TILEPROPERTY, out PropertyValue val) &&
                        !tile.Properties.TryGetValue(ModKeys.MAPSEATS_TILEPROPERTY, out val))
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
