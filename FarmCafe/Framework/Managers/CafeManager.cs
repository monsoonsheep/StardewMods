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
        internal static List<GameLocation> CafeLocations = new List<GameLocation>();
        public static List<List<string>> routesToCafe;

        internal static void populateRoutesToCafe()
        {
            routesToCafe = new List<List<string>>();
            foreach (string loc in new[] { "BusStop", "Town", "Beach" })
            {
                FindLocationRouteToCafe(GetLocationFromName(loc), CafeManager.CafeLocations.First());
            }
            foreach (var route in routesToCafe)
            {
                Debug.Log(string.Join(" - ", route));
            }
        }

        public static void FindLocationRouteToCafe(GameLocation startLocation, GameLocation endLocation)
        {
            Queue<string> frontier = new Queue<string>();
            frontier.Enqueue(startLocation.Name);

            Dictionary<string, string> cameFrom = new Dictionary<string, string>();
            cameFrom[startLocation.Name] = null;

            while (frontier.Count > 0)
            {
                string currentName = frontier.Dequeue();
                GameLocation current = GetLocationFromName(currentName);
                if (current == null)
                {
                    current = CafeLocations.First();
                }
                if (current.Name == endLocation.Name)
                    break;

                foreach (Warp warp in current.warps)
                {
                    string name = warp.TargetName;
                    if (!cameFrom.ContainsKey(name))
                    {
                        frontier.Enqueue(name);
                        cameFrom[name] = current.Name;
                    }
                }
                foreach (Point p in current.doors.Keys)
                {
                    string name = current.doors[p];
                    if (!cameFrom.ContainsKey(name))
                    {
                        frontier.Enqueue(name);
                        cameFrom[name] = current.Name;
                    }
                }
                if (current is BuildableGameLocation buildableCurrent)
                {
                    foreach (var building in buildableCurrent.buildings.Where(b => b.indoors.Value != null))
                    {
                        string name = building.indoors.Value.Name;
                        if (!cameFrom.ContainsKey(name))
                        {
                            frontier.Enqueue(name);
                            cameFrom[name] = current.Name;
                        }
                    }
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
            routesToCafe.Add(path);
        }

        internal static List<string> getLocationRoute(GameLocation start, GameLocation end)
        {
            foreach (var r in routesToCafe)
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
