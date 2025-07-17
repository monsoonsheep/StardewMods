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

    private readonly HelperInventory inventory;

    private LocationProvider locations = LocationProvider.Instance;

    private NPC? helper = null;
    private PathFindController? previousController = null;
    private MilkPail milkPail = null!;

    private int index = -1;

    internal int StartTime = 620;

    internal HelperState State = HelperState.OffDuty;

    private List<Job> plannedJobs = [];
    private Job? currentJob = null;

    private List<Building> buildingDoorsClosed = [];

    internal HelperManager()
    {
        Instance = this;

        this.inventory = Mod.HelperInventory;

        Mod.Events.GameLoop.SaveLoaded += this.OnSaveLoaded;
        Mod.Events.GameLoop.DayStarted += this.OnDayStarted;
        Mod.Events.GameLoop.TimeChanged += this.OnTimeChanged;
        Mod.Events.GameLoop.DayEnding += this.OnDayEnding;

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
        this.State = HelperState.OffDuty;
        this.currentJob = null;
        this.plannedJobs.Clear();
        this.index = -1;
        this.FindJobs();
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

    private void OnDayEnding(object? sender, DayEndingEventArgs e)
    {
        if (this.helper != null)
        {
            this.helper.EventActor = false;

            if (this.helper.currentLocation is Farm || this.helper.currentLocation is AnimalHouse)
            {
                WarpToForestWaitingPoint(this.helper);
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

            if (buildingType == "Barn" && indoors.animals.Any())
            {
                Log.Trace("Adding barn job");

                BarnJob job = new BarnJob(this.helper!, building);
                this.plannedJobs.Add(job);

                this.CheckBuildingAnimalDoor(building);
            }

            else if (buildingType == "Coop" && CoopJob.IsAvailable(building))
            {
                Log.Trace("Adding coop job");

                CoopJob job = new CoopJob(this.helper!, building);
                this.plannedJobs.Add(job);

                this.CheckBuildingAnimalDoor(building);
            }

            return true;
        });
    }

    private void CheckBuildingAnimalDoor(Building building)
    {
        if (building.animalDoorOpen.Value == true)
        {
            building.animalDoorOpen.Set(false);
            this.buildingDoorsClosed.Add(building);
        }
    }

    
    private void StartDay()
    {
        this.helper!.EventActor = true;

        // search for available jobs
        // if there's even one job, move to the start point
        // at the end of the start point

        this.previousController = this.helper!.controller;

        //this.helper.ignoreScheduleToday = true;

        GameLocation forest = this.locations.Forest;
        Point pos = new Point(67, 4);

        if (MoveHelper(forest, pos, this.StartJobs) == true)
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
            Log.Trace("All jobs finished");

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
            Log.Error("Path not found for job");

            this.NextJob();
            return;
        }
    }

    public static void OnFinishJob(Job job, NPC npc)
    {
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

    internal static Point EnterBuilding(NPC npc, Building building)
    {
        GameLocation indoors = building.GetIndoors();

        Point entry = ModUtility.GetEntryTileForBuildingIndoors(building);

        Game1.warpCharacter(npc, indoors, entry.ToVector2());

        return entry;
    }

    private void AllJobsFinished()
    {
        foreach (Building b in this.buildingDoorsClosed)
        {
            if (b.animalDoorOpen.Value == false)
            {
                b.animalDoorOpen.Set(true);
            }
        }

        this.GoHome();
    }

    private void GoHome()
    {
        MoveHelper(Mod.Locations.Farm, new Point(41, 61), this.LeaveFarmAndGoHome);
    }

    private void LeaveFarmAndGoHome(NPC npc)
    {
        WarpToForestWaitingPoint(npc);

        npc.EventActor = false;

        this.GoHomeFromForest();
    }

    private void GoHomeFromForest()
    {

    }

    internal static bool MoveHelper(GameLocation location, Point tile, Action<NPC> endBehavior)
    {
        NPC npc = Instance.helper!;

        // open all closed fences
        bool isFarm = location is Farm;
        List<Fence> fences = [];
        if (isFarm) {
            foreach (StardewValley.Object obj in Instance.locations.Farm.Objects.Values)
            {
                if (obj is Fence fence && fence.isGate.Value && fence.gatePosition.Value == 0)
                {
                    fence.toggleGate(true, is_toggling_counterpart: true);
                    fences.Add(fence);
                }
            }
        }

        bool res = false;

        Stack<Point>? path = Mod.Pathfinding.PathfindFromLocationToLocation(npc.currentLocation, npc.TilePoint, location, tile, null);
        if (path?.Any() == true)
        {
            npc.MoveTo(path, endBehavior);
            res = true;
        }

        // close the fences again
        if (res == true && isFarm)
        {
            foreach (Fence fence in fences)
            {
                fence.toggleGate(false, is_toggling_counterpart: true);
            }
        }

        return res;
    }

    private void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
    {
        
    }

    private void OnAssetReady(object? sender, AssetReadyEventArgs e)
    {
        
    }
}
