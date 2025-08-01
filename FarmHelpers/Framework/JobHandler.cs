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
using StardewMods.FarmHelpers.Framework.Enums;
using StardewMods.FarmHelpers.Framework.Jobs;
using StardewMods.SheepCore.Framework.Services;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.Inventories;
using StardewValley.Locations;
using StardewValley.Pathfinding;
using StardewValley.Tools;

namespace StardewMods.FarmHelpers.Framework;

internal class JobHandler
{
    internal static JobHandler Instance = null!;

    private readonly Worker worker = null!;
    private readonly Movement movement = null!;
    private readonly WorkerInventory inventory;

    private PathFindController? previousController = null;
    internal int StartTime = 620;
    private int index = -1;
    private List<Job> plannedJobs = [];
    private Job? currentJob = null;

    internal JobHandler()
    {
        Instance = this;

        this.worker = Mod.Worker;
        this.movement = Mod.Movement;
        this.inventory = Mod.HelperInventory;

        Mod.Events.GameLoop.DayStarted += this.OnDayStarted;
        Mod.Events.GameLoop.TimeChanged += this.OnTimeChanged;
        Mod.Events.GameLoop.DayEnding += this.OnDayEnding;

        Mod.Events.Content.AssetRequested += this.OnAssetRequested;
        Mod.Events.Content.AssetReady += this.OnAssetReady;
    }

    private void OnDayStarted(object? sender, DayStartedEventArgs e)
    {
        this.worker.State = WorkerState.OffDuty;
        this.currentJob = null;
        this.plannedJobs.Clear();
        this.index = -1;
        this.FindJobs();
    }

    private void OnTimeChanged(object? sender, TimeChangedEventArgs e)
    {
        if (this.worker.Npc != null)
        {
            this.worker.Npc.speed = 4;

            if (e.NewTime == this.StartTime)
            {
                this.StartDay();
            }
            else if (this.worker.State == WorkerState.MovingToFarm && !this.worker.Npc.currentLocation.farmers.Any())
            {
                this.worker.Npc.warpToPathControllerDestination();
            }
        }
    }

    private void OnDayEnding(object? sender, DayEndingEventArgs e)
    {
        if (this.worker.Npc != null)
        {
            Log.Debug("Setting EventActor to false");
            this.worker.Npc.EventActor = false;

            if (this.worker.Npc.currentLocation is Farm || this.worker.Npc.currentLocation is AnimalHouse)
            {
                this.WarpToForestWaitingPoint(this.worker.Npc);
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

                BarnJob job = new BarnJob(this.worker.Npc!, building, this.OnFinishJob);
                this.plannedJobs.Add(job);

                this.CloseAnimalDoor(building);
            }

            else if (buildingType == "Coop" && CoopJob.IsAvailable(building))
            {
                Log.Debug("Adding coop job");

                CoopJob job = new CoopJob(this.worker.Npc!, building, this.OnFinishJob);
                this.plannedJobs.Add(job);

                this.CloseAnimalDoor(building);
            }

            return true;
        });
    }

    // TODO remove maybe?
    private void CloseAnimalDoor(Building building)
    {
        building.animalDoorOpen.Set(false);
    }

    private void StartDay()
    {
        Log.Debug("Setting EventActor to true");

        this.worker.Npc!.EventActor = true;

        // search for available jobs
        // if there's even one job, move to the start point
        // at the end of the start point

        this.previousController = this.worker.Npc!.controller;

        //this.helper.ignoreScheduleToday = true;

        bool res = this.movement.Move(Mod.Locations.Forest, new Point(67, 4), this.StartJobs);

        if (res == true)
        {
            this.worker.State = WorkerState.MovingToFarm;
        }
        else
        {
            Log.Error("Path not found to the forest entry point, failed to start the day");
            this.worker.Npc.controller = this.previousController;
            this.worker.State = WorkerState.FailedToGoToWork;
        }
    }

    private void StartJobs(NPC npc)
    {
        // warp to Farm 41, 61, start jobs 
        this.WarpToFarmEntryPoint(npc);
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

        bool res = this.movement.Move(Mod.Locations.Farm, this.currentJob.StartPoint, this.currentJob.Start);

        if (res)
        {
            this.worker.State = WorkerState.MovingToJob;
        }
        else
        {
            Log.Error($"Path not found for job {this.currentJob.GetType().Name}");

            this.NextJob();
            return;
        }
    }

    internal void OnFinishJob(Job job)
    {
        Log.Debug($"{job.GetType().FullName} job finished");

        // 
        if (this.worker.Npc!.currentLocation != Mod.Locations.Farm || this.worker.Npc.TilePoint != job.StartPoint)
        {
            Game1.warpCharacter(this.worker.Npc, Mod.Locations.Farm, job.StartPoint.ToVector2());
        }

        // TODO track the job as finished, maybe stats?
        this.NextJob();
    }

    internal void WarpToForestWaitingPoint(NPC npc)
    {
        Game1.warpCharacter(npc, Mod.Locations.Forest, new Vector2(67, 4));
    }

    internal void WarpToFarmEntryPoint(NPC npc)
    {
        Game1.warpCharacter(npc, Mod.Locations.Farm, new Vector2(41, 61));
    }

    private void AllJobsFinished()
    {
        this.GoHome();
    }

    private void GoHome()
    {
        this.movement.Move(Mod.Locations.Farm, new Point(41, 61), this.LeaveFarmAndGoHome);
    }

    private void LeaveFarmAndGoHome(NPC npc)
    {
        this.WarpToForestWaitingPoint(npc);

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
