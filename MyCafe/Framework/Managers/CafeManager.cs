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
using HarmonyLib;
using System.Reflection;
using StardewValley.Buildings;

namespace MyCafe.Framework.Managers;

internal sealed class CafeManager
{
    internal static CafeManager Instance;

    internal GameLocation CafeIndoors;

    internal int OpeningTime = 1200;
    internal int ClosingTime = 2100;

    internal CafeManager() => Instance = this;

    internal void DayUpdate(object sender, DayStartedEventArgs e)
    {
        UpdateCafeIndoorLocation();
    }

    internal void OnTimeChanged(object sender, TimeChangedEventArgs e)
    {

    }

    internal void UpdateCafeIndoorLocation()
    {
        CafeIndoors = Game1.getLocationFromName("MonsoonSheep.MyCafe_CafeBuilding", isStructure: true);
        if (CafeIndoors == null)
        {
            var cafeBuilding = Game1.getFarm().buildings.FirstOrDefault(x => x.GetData()?.CustomFields.TryGetValue("MonsoonSheep.MyCafe_IsCafeBuilding", out string result) == true && result == "true");
            if (cafeBuilding != null)
            {
					CafeIndoors = cafeBuilding.GetIndoors();
					// if (CafeIndoors != null) Game1._locationLookup[CafeIndoors.Name] = CafeIndoors;
            }
        }
    }

    internal void PopulateRoutesToCafe()
    {
        List<GameLocation> cafeLocations = new List<GameLocation>() { Game1.getFarm() };
        if (CafeIndoors != null)
        {
            cafeLocations.Add(CafeIndoors);
        }

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
                route = WarpPathfindingCache.GetLocationRoute(location.Name, "BusStop", NPC.female)?.ToList();
                reverseRoute = WarpPathfindingCache.GetLocationRoute("BusStop", location.Name, NPC.female)?.ToList();
            }

            if (route == null || reverseRoute == null)
                continue;

            route.Add("Farm");
            reverseRoute.Insert(0, "Farm");

            if (route.Count <= 1)
                continue;

            MethodInfo method = AccessTools.Method(typeof(WarpPathfindingCache), "AddRoute", new[] { typeof(List<string>), typeof(int?) });
            if (method == null)
            {
                Log.Error("Couldn't find method to add route");
                return;
            }

            method.Invoke(null, new[] { route, null });
            method.Invoke(null, new[] { reverseRoute, null });

            if (CafeIndoors != null)
            {
                route.Add(CafeIndoors.Name);
                reverseRoute.Insert(0, CafeIndoors.Name);

                method.Invoke(null, new[] { route, null });
                method.Invoke(null, new[] { reverseRoute, null });
            }
        }
    }
}