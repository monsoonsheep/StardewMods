using StardewValley.Locations;
using StardewValley;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static FarmCafe.Framework.Utility;

namespace FarmCafe.Framework.Managers
{
    internal partial class CafeManager
    {
        internal static CafeLocation GetCafeLocation()
        {
            return CafeLocations.OfType<CafeLocation>().FirstOrDefault();
        }

        internal static bool UpdateCafeLocation()
        {
            CafeLocation foundCafe = Game1.getFarm().buildings
                .FirstOrDefault(b => b.indoors.Value is CafeLocation)
                ?.indoors.Value as CafeLocation;

            CafeLocation cachedCafeLocation = GetCafeLocation();

            if (foundCafe == null)
            {
                CafeLocations.Clear();
                CafeLocations.Add(Game1.getFarm());
                return false;
            }

            if (cachedCafeLocation == null)
            {
                CafeLocations.Add(foundCafe);
            }
            else if (!foundCafe.Equals(cachedCafeLocation))
            {
                foreach (var table in CafeManager.Tables)
                {
                    if (table.CurrentLocation.Equals(cachedCafeLocation))
                    {
                        table.CurrentLocation = foundCafe;
                    }
                }
                CafeLocations.Remove(cachedCafeLocation);
                CafeLocations.Add(foundCafe);
            }

            if (CafeLocations.Count == 0)
            {
                CafeLocations.Add(Game1.getFarm());
            }
            else
            {
                GetCafeLocation()?.PopulateMapTables();
            }

            return true;
        }

        internal void PopulateRoutesToCafe()
        {
            if (CafeLocations.Count == 0)
                return;

            RoutesToCafe = new List<List<string>>();
            foreach (string loc in new[] { "BusStop", "Farm" })
            {
                foreach (var cafe in CafeLocations)
                {
                    RoutesToCafe.Add(
                        FindLocationRouteToCafe(GetLocationFromName(loc), cafe)
                        );
                }
            }

            var routesFromBus = RoutesToCafe.Where(r => r.First().Equals("BusStop"));

            var routesToBus = ModEntry.ModHelper.Reflection
                .GetField<List<List<string>>>(typeof(NPC), "routesFromLocationToLocation").GetValue()
                .Where(route => route.Last() is "BusStop").Select(r => r.SkipLast(1));

            var routesToAdd = (
                from route in routesToBus 
                from busRoute in routesFromBus 
                select route.Concat(busRoute).ToList());

            RoutesToCafe = RoutesToCafe.Concat(routesToAdd).ToList();
            //RoutesToCafe.ForEach((route) => Logger.Log(string.Join(" - ", route)));
        }

        public List<string> FindLocationRouteToCafe(GameLocation startLocation, GameLocation endLocation)
        {
            if (startLocation.Equals(endLocation))
            {
                return new List<string>() { startLocation.Name };
            }
            var frontier = new Queue<string>();
            frontier.Enqueue(startLocation.Name);

            var cameFrom = new Dictionary<string, string>
            {
                [startLocation.Name] = null
            };

            while (frontier.Count > 0)
            {
                string currentName = frontier.Dequeue();
                GameLocation current = GetLocationFromName(currentName);

                if (current == null)
                    continue;
                if (current.Name == endLocation.Name)
                    break;

                foreach (var name in current.warps.Select(warp => warp.TargetName)
                             .Where(name => !cameFrom.ContainsKey(name)))
                {
                    frontier.Enqueue(name);
                    cameFrom[name] = current.Name;
                }

                foreach (var name in current.doors.Keys.Select(p => current.doors[p])
                             .Where(name => !cameFrom.ContainsKey(name)))
                {
                    frontier.Enqueue(name);
                    cameFrom[name] = current.Name;
                }

                if (current is not BuildableGameLocation buildableCurrent) continue;

                foreach (var building in buildableCurrent.buildings.Where(b => b.indoors.Value != null))
                {
                    string name = building.indoors.Value.Name;
                    if (cameFrom.ContainsKey(name)) continue;

                    frontier.Enqueue(name);
                    cameFrom[name] = current.Name;
                }
            }

            List<string> path = new List<string>() { endLocation.Name };
            string point = endLocation.Name;
            while (true)
            {
                if (cameFrom.ContainsKey(point))
                {
                    path.Add(cameFrom[point]);
                    point = cameFrom[point];
                    if (point == startLocation.Name) break;
                }
                else
                {
                    return null;
                }
            }

            path.Reverse();
            return path;
        }

        internal List<string> GetLocationRoute(GameLocation start, GameLocation end)
        {
            List<string> route = GameRoutes?.FirstOrDefault(
                r => r.First() == start.Name && r.Last() == end.Name
            )?.ToList();

            if (route == null)
                route = RoutesToCafe.FirstOrDefault(
                r => r.First() == start.Name && r.Last() == end.Name
            )?.ToList();

            if (route == null)
            {
                route = RoutesToCafe.FirstOrDefault(
                    r => r.First() == end.Name && r.Last() == start.Name
                )?.ToList();
                route?.Reverse();
            }

            return route;
        }

    }
}
