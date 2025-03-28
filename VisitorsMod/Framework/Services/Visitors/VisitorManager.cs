using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewMods.VisitorsMod.Framework.Data;
using StardewMods.VisitorsMod.Framework.Enums;
using HarmonyLib;
using StardewMods.VisitorsMod.Framework.Data.Models;
using StardewMods.VisitorsMod.Framework.Data.Models.Activities;
using StardewModdingAPI;
using StardewMods.VisitorsMod.Framework.Services.Visitors.RandomVisitors;
using StardewMods.VisitorsMod.Framework.Interfaces;
using StardewMods.VisitorsMod.Framework.Services.Visitors.Spawners;
using StardewMods.SheepCore.Framework.Services;
using StardewValley.Pathfinding;

namespace StardewMods.VisitorsMod.Framework.Services.Visitors;

internal class VisitorManager
{
    // Services
    private ActivityManager activities = null!;
    private RandomVisitorBuilder visitorBuilder = null!;

    // State
    private List<ISpawner> spawners = null!;
    private Dictionary<int, List<Visit>> visitSchedule = [];

    // Properties
    private IEnumerable<Visit> OngoingVisits
        => this.visitSchedule.Values.SelectMany(i => i).Where(v => v.state == VisitState.Ongoing);

    private ISpawner? GetSpawner(string name)
        => this.spawners.FirstOrDefault(s => s.Id.Equals(name));

    internal void Initialize()
    {
        Mod.Events.GameLoop.SaveLoaded += this.OnSaveLoaded;
        Mod.Events.GameLoop.ReturnedToTitle += this.OnReturnedToTitle;
    }

    private void OnReturnedToTitle(object? sender, ReturnedToTitleEventArgs e)
    {
        Mod.Events.GameLoop.DayStarted -= this.OnDayStarted;
        Mod.Events.GameLoop.TimeChanged -= this.OnTimeChanged;
    }

    private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
    {
        if (!Context.IsMainPlayer)
            return;
        
        this.activities = new ActivityManager();
        this.activities.Initialize();
        this.visitorBuilder = new RandomVisitorBuilder();

        this.spawners = [
            new TrainSpawner(),
            new RoadSpawner(),
            new WarpSpawner(),
            ];

        IBusSchedulesApi? busSchedules = Mod.ModHelper.ModRegistry.GetApi<IBusSchedulesApi>("MonsoonSheep.BusSchedules");
        if (busSchedules != null)
            this.spawners.Add(new BusSpawner(busSchedules));
        
        Mod.Events.GameLoop.DayStarted += this.OnDayStarted;
        Mod.Events.GameLoop.TimeChanged += this.OnTimeChanged;
    }

    private void OnDayStarted(object? sender, DayStartedEventArgs e)
    {
        this.visitSchedule.Clear();

        // Create visits for today

        Log.Trace("Activities today:");

        List<ActivityModel> activitiesForToday = this.activities.GetActivitiesForToday();
        foreach (ActivityModel activity in activitiesForToday)
        {
            // Select a random spawner out of available spawners
            ISpawner? spawner = activity.ArriveBy
                .Select(this.GetSpawner)
                .Where(s => s != null && s.IsAvailable())
                .MinBy(_ => Game1.random.Next());

            if (spawner == null)
            {
                Log.Warn($"Activity {activity.Id} doesn't have a valid ArriveBy: {activity.ArriveBy}");
                continue;
            }

            int timeSelected = Utility.ConvertMinutesToTime(
                (Game1.random.Next(
                    Utility.ConvertTimeToMinutes(activity.TimeRange[0]),
                    Utility.ConvertTimeToMinutes(activity.TimeRange[1])
                    ) / 10) * 10
            );

            // Create new list if not exist
            if (!this.visitSchedule.ContainsKey(timeSelected) || this.visitSchedule[timeSelected] == null)
                this.visitSchedule[timeSelected] = [];

            Visit visit = new Visit(
                activity,
                spawner,
                startTime: timeSelected,
                endTime: Utility.ModifyTime(timeSelected, ModUtility.GetDurationValue(activity.Duration)));

            this.visitSchedule[timeSelected].Add(visit);
            Log.Trace($" - {activity.Id} at {timeSelected} by {spawner.Id}");
        }
    }

    private void OnTimeChanged(object? sender, TimeChangedEventArgs e)
    {
        // Start scheduled visits
        if (this.visitSchedule.ContainsKey(Game1.timeOfDay))
        {
            foreach (Visit visit in this.visitSchedule[Game1.timeOfDay])
            {
                if (visit.state == VisitState.NotStarted)
                {
                    this.CreateRandomNpcsForVisit(visit);
                    this.StartVisit(visit);
                }
            }
        }

        // End visits that are due to end
        foreach (Visit visit in this.OngoingVisits)
        {
            if (Game1.timeOfDay >= visit.endTime)
            {
                this.EndVisit(visit);
            }
        }
    }

    private void CreateRandomNpcsForVisit(Visit visit)
    {
        visit.group = [];

        // Create NPCs
        foreach (var actor in visit.activity.Actors)
        {
            NPC npc = this.CreateRandomNpc();

            visit.group.Add(npc);
        }
    }

    private NPC CreateRandomNpc()
    {
        VisitorModel model = this.visitorBuilder.GenerateRandomVisitor();

        Texture2D portrait = Game1.content.Load<Texture2D>(model.Portrait);
        AnimatedSprite sprite = new AnimatedSprite(model.Spritesheet, 0, 16, 32);
        NPC npc = new NPC(
            sprite,
            new Vector2(7, 24) * 64f,
            "Town",
            2,
            Values.RANDOMVISITOR_NAMEPREFIX + model.Name,
            false,
            portrait);

        // don't load portraits again
        npc.portraitOverridden = true;

        // for ExtraNpcBehaviors sitting
        npc.modData[Values.DATA_SITTINGSPRITES] = "19 17 16 18";

        return npc;
    }

    private bool StartVisit(Visit visit)
    {
        static void cancel(Visit visit)
        {
            visit.state = VisitState.Failed;
            Log.Trace($"Visit failed {visit.activity.Id} at {visit.spawner.Id}");
            foreach (NPC npc in visit.group)
            {
                npc.currentLocation?.characters.Remove(npc);
                npc.currentLocation = null;
            }
        }

        // Spawn
        if (!visit.spawner.SpawnVisitors(visit))
        {
            cancel(visit);
            return false;
        }

        // Pathfind to target
        string targetLocation = visit.activity.Location;

        for (int i = 0; i < visit.group.Count; i++)
        {
            NPC npc = visit.group[i];

            string behaviorName = visit.activity.Actors[i].Behavior;

            // End behavior can be something from ExtraNpcBehaviors
            PathFindController.endBehavior endBehavior = (n, _) => (n as NPC)!.StartActivityRouteEndBehavior(behaviorName, null);

            // Pathfind to target
            if (!npc.MoveTo(Game1.getLocationFromName(targetLocation), visit.activity.Actors[i].TilePosition, endBehavior))
            {
                cancel(visit);
                return false;
            }
        }

        visit.state = VisitState.HeadingToDestination;

        // Spawner-specific action
        visit.spawner.AfterSpawn(visit);

        return true;
    }

    private void EndVisit(Visit visit)
    {
        if (!visit.spawner.EndVisit(visit))
        {
            Log.Error("NPCs couldn't path back");
            for (int i = 0; i < visit.group.Count; i++)
            {
                NPC npc = visit.group[i];

                npc.currentLocation.characters.Remove(npc);
                npc.currentLocation = null;
                visit.state = VisitState.Ended;
            }

            return;
        }

        visit.state = VisitState.HeadingBack;

        // End behavior: delete after reaching destination
        for (int i = 0; i < visit.group.Count; i++)
        {
            NPC npc = visit.group[i];

            npc.controller.endBehaviorFunction = delegate (Character c, GameLocation loc)
            {
                if (c is NPC n)
                {
                    n.currentLocation.characters.Remove(n);
                    n.currentLocation = null;

                    if (visit.group.All(i => i.currentLocation == null))
                    {
                        visit.state = VisitState.Ended;
                    }
                }
            };
        }
    }
}
