using HarmonyLib;
using MyCafe.Framework.Customers;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Pathfinding;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MyCafe.Framework.ChairsAndTables;
using SUtility = StardewValley.Utility;

namespace MyCafe.Framework.Managers;

internal sealed class CafeManager
{
    internal static CafeManager Instance;

    internal GameLocation CafeIndoors;

    internal int OpeningTime = 1200;
    internal int ClosingTime = 2100;
    internal int LastTimeCustomersArrived = 0;

    internal CafeManager() => Instance = this;

    internal void DayUpdate(object sender, DayStartedEventArgs e)
    {
        UpdateCafeIndoorLocation();
    }

    internal void SpawnCustomers()
    {
        Table table = Mod.Tables.CurrentTables.MinBy(t => t.Seats.Count);
        if (table == null)
        {
            Log.Debug("No tables available");
            return;
        }
    }

    internal void OnTimeChanged(object sender, TimeChangedEventArgs e)
    {
        int minutesTillCloses = SUtility.CalculateMinutesBetweenTimes(Game1.timeOfDay, ClosingTime);
        int minutesTillOpens = SUtility.CalculateMinutesBetweenTimes(Game1.timeOfDay, OpeningTime);
        int minutesSinceLastVisitors = SUtility.CalculateMinutesBetweenTimes(LastTimeCustomersArrived, Game1.timeOfDay);
        float percentageOfTablesFree = (float)Mod.Tables.CurrentTables.Count(t => !t.IsReserved) / Mod.Tables.CurrentTables.Count;

        if (minutesTillCloses <= 20)
            return;

        float prob = 0f;

        // more chance if it's been a while since last Visitors
        prob += minutesSinceLastVisitors switch
        {
            <= 20 => 0f,
            <= 30 => Game1.random.Next(5) == 0 ? 0.05f : -0.1f,
            <= 60 => Game1.random.Next(2) == 0 ? 0.1f : 0f,
            _ => 0.25f
        };

        // more chance if a higher percent of tables are free
        prob += percentageOfTablesFree switch
        {
            <= 0.2f => 0.0f,
            <= 0.5f => 0.1f,
            <= 0.8f => 0.15f,
            _ => 0.2f
        };

        // slight chance to spawn if last hour of open time
        if (minutesTillCloses <= 60)
            prob += (Game1.random.Next(20 + Math.Max(0, (minutesTillCloses / 3))) >= 28) ? 0.2f : -0.5f;
    }

    internal void UpdateCafeIndoorLocation()
    {
        GameLocation indoors = Game1.getLocationFromName("MonsoonSheep.MyCafe_CafeBuilding", isStructure: true);
        if (indoors == null)
        {
            var cafeBuilding = Game1.getFarm().buildings.FirstOrDefault(x => x.GetData()?.CustomFields.TryGetValue("MonsoonSheep.MyCafe_IsCafeBuilding", out string result) == true && result == "true");
            if (cafeBuilding != null)
            {
                indoors = cafeBuilding.GetIndoors();
            }
        }
        if (indoors != null)
            CafeIndoors = indoors;
        // if (CafeIndoors != null) Game1._locationLookup[CafeIndoors.Name] = CafeIndoors;
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