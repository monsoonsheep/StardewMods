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

internal class Worker
{
    internal static Worker Instance = null!;

    private readonly HelperInventory inventory;

    internal NPC? npc;

    private PathFindController? previousController = null;

    internal int StartTime = 620;

    internal HelperState State = HelperState.OffDuty;

    private int index = -1;
    private List<Job> plannedJobs = [];
    private Job? currentJob = null;

    internal Worker()
    {
        Instance = this;

        this.inventory = Mod.HelperInventory;

        Mod.Events.GameLoop.SaveLoaded += this.OnSaveLoaded;
        Mod.Events.GameLoop.DayStarted += this.OnDayStarted;
        Mod.Events.GameLoop.TimeChanged += this.OnTimeChanged;
        Mod.Events.GameLoop.DayEnding += this.OnDayEnding;

        Mod.Events.Content.AssetRequested += this.OnAssetRequested;
        Mod.Events.Content.AssetReady += this.OnAssetReady;

        Mod.Harmony.Patch(
            AccessTools.Method(typeof(Character), nameof(Character.collideWith), [typeof(StardewValley.Object)]),
            postfix: new HarmonyMethod(this.GetType(), nameof(After_CharacterCollideWith))
            );
    }

    private static void After_CharacterCollideWith(Character __instance, StardewValley.Object o, ref bool __result)
    {
        if (__instance == Instance.npc && o is Fence fence && fence.isGate.Value)
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
                this.npc = Game1.getCharacterFromName(pair.Key);
            }
        }
    }

    private void OnDayStarted(object? sender, DayStartedEventArgs e)
    {
        this.State = HelperState.OffDuty;
        this.currentJob = null;
        this.plannedJobs.Clear();
        this.index = -1;
        this.FindJobs();
    }

    private void OnTimeChanged(object? sender, TimeChangedEventArgs e)
    {
        if (this.npc != null)
        {
            this.npc.speed = 4;

            if (e.NewTime == this.StartTime)
            {
                this.StartDay();
            }
            else if (this.State == HelperState.MovingToFarm && !this.npc.currentLocation.farmers.Any())
            {
                this.npc.warpToPathControllerDestination();
            }
        }
    }

    private void OnDayEnding(object? sender, DayEndingEventArgs e)
    {
        if (this.npc != null)
        {
            Log.Debug("Setting EventActor to false");
            this.npc.EventActor = false;

            if (this.npc.currentLocation is Farm || this.npc.currentLocation is AnimalHouse)
            {
                WarpToForestWaitingPoint(this.npc);
            }
        }
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

            if (buildingType == "Barn" && BarnJob.IsAvailable(building))
            {
                Log.Debug("Adding barn job");

                BarnJob job = new BarnJob(this.npc!, building, OnFinishJob);
                this.plannedJobs.Add(job);

                this.CloseAnimalDoor(building);
            }

            else if (buildingType == "Coop" && CoopJob.IsAvailable(building))
            {
                Log.Debug("Adding coop job");

                CoopJob job = new CoopJob(this.npc!, building, OnFinishJob);
                this.plannedJobs.Add(job);

                this.CloseAnimalDoor(building);
            }

            return true;
        });
    }

    private void CloseAnimalDoor(Building building)
    {
        if (building.animalDoorOpen.Value == true)
        {
            building.animalDoorOpen.Set(false);
        }
    }

    private void StartDay()
    {
        Log.Debug("Setting EventActor to true");

        this.npc!.EventActor = true;

        // search for available jobs
        // if there's even one job, move to the start point
        // at the end of the start point

        this.previousController = this.npc!.controller;

        //this.helper.ignoreScheduleToday = true;

        bool res = MoveHelper(Mod.Locations.Forest, new Point(67, 4), this.StartJobs);

        if (res == true)
        {
            this.State = HelperState.MovingToFarm;
        }
        else
        {
            Log.Error("Path not found to the forest entry point, failed to start the day");
            this.npc.controller = this.previousController;
            this.State = HelperState.FailedToGoToWork;
        }
    }

    private void StartJobs(NPC npc)
    {
        // warp to Farm 41, 61, start jobs 
        WarpToFarmEntryPoint(npc);
        this.NextJob();
    }

    private void NextJob()
    {
        this.index += 1;

        if (this.index >= this.plannedJobs.Count)
        {
            Log.Debug("All jobs finished");

            this.AllJobsFinished();
            return;
        }

        this.currentJob = this.plannedJobs[this.index];

        bool res = MoveHelper(Mod.Locations.Farm, this.currentJob.StartPoint, this.currentJob.Start);

        if (res)
        {
            this.State = HelperState.MovingToJob;
        }
        else
        {
            Log.Error($"Path not found for job {this.currentJob.GetType().Name}");

            this.NextJob();
            return;
        }
    }

    public static void OnFinishJob(Job job)
    {
        Log.Debug($"{job.GetType().FullName} job finished");

        if (Instance.npc!.currentLocation != Mod.Locations.Farm || Instance.npc.TilePoint != job.StartPoint)
        {
            Game1.warpCharacter(Instance.npc, Mod.Locations.Farm, job.StartPoint.ToVector2());
        }

        // TODO track the job as finished, maybe stats?
        Instance.NextJob();
    }

    internal static void WarpToForestWaitingPoint(NPC npc)
    {
        Game1.warpCharacter(npc, Mod.Locations.Forest, new Vector2(67, 4));
    }

    internal static void WarpToFarmEntryPoint(NPC npc)
    {
        Game1.warpCharacter(npc, Mod.Locations.Farm, new Vector2(41, 61));
    }

    private void AllJobsFinished()
    {
        this.GoHome();
    }

    private void GoHome()
    {
        MoveHelper(Mod.Locations.Farm, new Point(41, 61), this.LeaveFarmAndGoHome);
    }

    private void LeaveFarmAndGoHome(NPC npc)
    {
        WarpToForestWaitingPoint(npc);

        Log.Debug("Setting EventActor to false");
        npc.EventActor = false;

        this.GoHomeFromForest();
    }

    private void GoHomeFromForest()
    {

    }

    private void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
    {
        
    }

    private void OnAssetReady(object? sender, AssetReadyEventArgs e)
    {
        
    }
}
