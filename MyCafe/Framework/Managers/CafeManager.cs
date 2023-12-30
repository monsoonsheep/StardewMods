using System;
using StardewValley;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework.Graphics;
using MyCafe.Framework.Customers;
using StardewModdingAPI;
using xTile.Tiles;
using Point = Microsoft.Xna.Framework.Point;
using StardewValley.Menus;
using StardewValley.Pathfinding;
using MyCafe.Framework.Objects;
using StardewModdingAPI.Events;
using xTile.Layers;
using xTile.ObjectModel;
using StardewValley.Objects;

namespace MyCafe.Framework.Managers
{
    internal sealed class CafeManager
    {
        internal static CafeManager Instance;

        internal Dictionary<string, List<string[]>> RoutesToCafe;
        internal GameLocation CafeIndoors;
        
        internal int OpeningTime = 1200;
        internal int ClosingTime = 2100;

        private CustomerManager customers;
        private MenuManager menu;
        private TableManager tables;
        private AssetManager assets;

        internal CafeManager()
        {
            customers = CustomerManager.Instance;
            menu = MenuManager.Instance;
            tables = TableManager.Instance;
            assets = AssetManager.Instance;

            UpdateCafeIndoorLocation();
            PopulateRoutesToCafe();

            Instance = this;
        }

        internal void DayUpdate(object sender, DayStartedEventArgs e)
        {
            UpdateCafeIndoorLocation();
        }

        internal void OnTimeChanged(object sender, TimeChangedEventArgs e)
        {

        }

        internal void UpdateCafeIndoorLocation()
        {
            CafeIndoors = Game1.getLocationFromName("MonsoonSheep.MyCafe_CafeLocation");
        }

        internal void PopulateRoutesToCafe()
        {
            List<GameLocation> cafeLocations = new List<GameLocation>() { Game1.getFarm() };

            var busStop = Game1.getLocationFromName("BusStop");

            RoutesToCafe = new Dictionary<string, List<string[]>>();
            if (CafeIndoors != null)
            {
                cafeLocations.Add(CafeIndoors);
                RoutesToCafe[CafeIndoors.NameOrUniqueName] = new List<string[]>();
            }
            RoutesToCafe["Farm"] = new List<string[]>();

            foreach (var location in Game1.locations)
            {
                List<string> route;
                List<string> reverseRoute;

                if (location.Name.Equals("BusStop"))
                {
                    route = new List<string>() { "BusStop" };
                    reverseRoute = new List<string>() { "BusStop" };
                }
                else
                {
                    route = WarpPathfindingCache.GetLocationRoute(location.NameOrUniqueName, "BusStop", NPC.female)?.ToList();
                    reverseRoute = WarpPathfindingCache.GetLocationRoute("BusStop", location.NameOrUniqueName, NPC.female)?.ToList();
                }
              
                if (route == null || reverseRoute == null)
                    continue;

                RoutesToCafe[location.NameOrUniqueName] = new List<string[]>();

                route.Add("Farm");
                reverseRoute.Insert(0, "Farm");

                if (route.Count <= 1)
                    continue;

                RoutesToCafe[location.NameOrUniqueName].Add(route.ToArray());
                RoutesToCafe["Farm"].Add(reverseRoute.ToArray());

                if (CafeIndoors != null)
                {
                    route.Add(CafeIndoors.NameOrUniqueName);
                    reverseRoute.Insert(0, CafeIndoors.NameOrUniqueName);

                    RoutesToCafe[location.NameOrUniqueName].Add(route.ToArray());
                    RoutesToCafe[CafeIndoors.NameOrUniqueName].Add(reverseRoute.ToArray());
                }
            }
        }

        internal string[] GetLocationRoute(GameLocation startingLocation, GameLocation targetLocation)
        {
            string[] result = WarpPathfindingCache.GetLocationRoute(startingLocation.NameOrUniqueName, targetLocation.NameOrUniqueName, NPC.female);
            if (result != null)
                return result;

            if (RoutesToCafe.TryGetValue(startingLocation.NameOrUniqueName, out List<string[]> routes))
            {
                foreach (var route in routes)
                {
                    if (route[^1].Equals(targetLocation.NameOrUniqueName))
                    {
                        return route;
                    }
                }
            }

            return null;
        }
    }
}