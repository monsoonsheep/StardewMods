using StardewValley;
using StardewValley.Locations;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static FarmCafe.Framework.Utilities.Utility;

namespace FarmCafe.Framework.Managers
{
    internal static class CafeManager
    {
        internal static List<GameLocation> CafeLocations = new();
        public static List<List<string>> RoutesToCafe;

        internal static void PopulateRoutesToCafe()
        {
            RoutesToCafe = new List<List<string>>();
            foreach (string loc in new[] { "BusStop", "Town", "Beach" , "Farm" })
            {
                FindLocationRouteToCafe(GetLocationFromName(loc), CafeManager.CafeLocations.First());
            }
            foreach (var route in RoutesToCafe)
            {
                Debug.Log(string.Join(" - ", route));
            }
        }

        public static void FindLocationRouteToCafe(GameLocation startLocation, GameLocation endLocation)
        {
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
               
                if (current.Name == endLocation.Name)
                    break;

                foreach (var name in current.warps.Select(warp => warp.TargetName).Where(name => !cameFrom.ContainsKey(name)))
                {
                    frontier.Enqueue(name);
                    cameFrom[name] = current.Name;
                }

                foreach (var name in current.doors.Keys.Select(p => current.doors[p]).Where(name => !cameFrom.ContainsKey(name)))
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
                    return;
                }

            }
            path.Reverse();
            RoutesToCafe.Add(path);
        }

        internal static List<string> GetLocationRoute(GameLocation start, GameLocation end)
        {
            foreach (var r in RoutesToCafe)
            {
                if (r.First() == start.Name && r.Last() == end.Name)
                {
                    return r;
                }
            }

            return null;
        }

    }
}
