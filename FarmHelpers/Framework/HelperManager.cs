using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Force.DeepCloner;
using HarmonyLib;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewMods.SheepCore.Framework.Services;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.Inventories;
using StardewValley.Locations;
using StardewValley.Pathfinding;
using StardewValley.Tools;

namespace StardewMods.FarmHelpers.Framework;

internal class HelperManager
{
    internal static HelperManager Instance = null!;

    private LocationProvider locations = LocationProvider.Instance;

    private NPC? helper = null;
    private PathFindController? previousController = null;
    private MilkPail milkPail = null!;

    private List<Building> buildings = [];
    private int index = -1;

    internal int StartTime = 620;

    internal HelperState State = HelperState.OffDuty;

    internal List<Item> HelperInventory = [];

    private List<Job> plannedJobs = [];
    private Job? currentJob = null;

    internal HelperManager()
    {
        Instance = this;

        Mod.Events.GameLoop.SaveLoaded += this.OnSaveLoaded;
        Mod.Events.GameLoop.DayStarted += this.OnDayStarted;
        Mod.Events.GameLoop.TimeChanged += this.OnTimeChanged;
        Mod.Events.Content.AssetRequested += this.OnAssetRequested;
        Mod.Events.Content.AssetReady += this.OnAssetReady;

        this.milkPail = new MilkPail();

        Mod.Harmony.Patch(
            AccessTools.Method(typeof(Character), nameof(Character.collideWith), [typeof(StardewValley.Object)]),
            postfix: new HarmonyMethod(this.GetType(), nameof(After_CharacterCollideWith))
            );
    }

    private static void After_CharacterCollideWith(Character __instance, StardewValley.Object o, ref bool __result)
    {
        if (__instance == Instance.helper && o is Fence fence && fence.isGate.Value)
        {
            __result = false;
        }
    }

    private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
    {
        foreach (var pair in Game1.characterData)
        {
            if (pair.Value.CustomFields != null
                && pair.Value.CustomFields.TryGetValue("Mods/MonsoonSheep.FarmHelpers/HelperNpc", out string? val)
                && val.ToLower() == "true")
            {
                this.helper = Game1.getCharacterFromName(pair.Key);
            }
        }
    }

    private void OnDayStarted(object? sender, DayStartedEventArgs e)
    {
        this.index = 0;
        this.FindJobs();
    }

    private void FindJobs()
    {
        Utility.ForEachBuilding((building) =>
        {
            if (building.GetParentLocation() is not Farm)
            {
                return true;
            }

            GameLocation? indoors = building.GetIndoors();
            if (indoors == null)
            {
                return true;
            }

            string buildingType = building.buildingType.Value;

            if (buildingType == "Barn" && indoors.animals.Any())
            {
                return true;
            }

            else if (buildingType == "Coop" && CoopJob.IsAvailable(building))
            {
                CoopJob job = new CoopJob(this.helper!, building);
                this.plannedJobs.Add(job);

            }

            return true;
        });
    }

    private void OnTimeChanged(object? sender, TimeChangedEventArgs e)
    {
        if (this.helper != null)
        {
            this.helper.speed = 4;

            if (e.NewTime == this.StartTime)
            {
                this.StartDay();
            }
            else if (this.State == HelperState.MovingToFarm && !this.helper.currentLocation.farmers.Any())
            {
                this.helper.warpToPathControllerDestination();
            }
        }
    }

    private void StartDay()
    {
        // search for available jobs
        // if there's even one job, move to the start point
        // at the end of the start point

        this.previousController = this.helper!.controller;

        //this.helper.ignoreScheduleToday = true;

        GameLocation forest = this.locations.Forest;
        Point pos = new Point(67, 4);

        if (this.helper.MoveTo(forest, pos, endBehavior: this.StartJobs) == true)
        {
            this.State = HelperState.MovingToFarm;
        }
        else
        {
            Log.Error("Path not found");
            this.helper.controller = this.previousController;
            this.State = HelperState.FailedToGoToWork;
        }
    }

    private void StartJobs(NPC helper)
    {
        // warp to Farm 41, 61, start jobs 
        // Helper jobs -> Coop take (eggs, duck feather, rabbit foot), barn milk cows, fish pond (in the future)

        Game1.warpCharacter(helper, this.locations.Farm, new Vector2(41, 61));

        this.NextJob();
    }

    private void NextJob()
    {
        this.index += 1;

        if (this.index >= this.plannedJobs.Count)
        {
            this.GoHome();
            return;
        }

        this.currentJob = this.plannedJobs[this.index];

        bool res = this.helper!.MoveTo(Mod.Locations.Farm, this.currentJob.StartPoint, endBehavior: this.currentJob.Start);

        if (res)
        {
            this.State = HelperState.MovingToJob;
        }
        else
        {
            Log.Error("Path not found");
            this.NextJob();
            return;
        }
    }

    private void StartJob()
    {

    }

    private void GoHome()
    {

    }

    private void OpenAllGates()
    {
        foreach (StardewValley.Object obj in this.locations.Farm.Objects.Values)
        {
            if (obj is Fence fence && fence.isGate.Value)
            {
                fence.isTemporarilyInvisible = true;
            }
        }
    }

    private void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
    {
        
    }

    private void OnAssetReady(object? sender, AssetReadyEventArgs e)
    {
        
    }
}
