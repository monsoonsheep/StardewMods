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

namespace StardewMods.VisitorsMod.Framework.Services.Visitors;

internal class VisitorManager
{
    internal static VisitorManager Instance = null!;

    // Services
    private ActivityManager activities = null!;
    private RandomVisitorBuilder visitorBuilder = null!;

    // State
    private List<ISpawner> spawners = null!;
    private Dictionary<int, List<Visit>> visitSchedule = [];

    public VisitorManager()
        => Instance = this;

    internal void Initialize()
    {
        ModEntry.Events.GameLoop.SaveLoaded += this.OnSaveLoaded;
        ModEntry.Events.GameLoop.ReturnedToTitle += this.OnReturnedToTitle;
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

        IBusSchedulesApi? busSchedules = ModEntry.Helper.ModRegistry.GetApi<IBusSchedulesApi>("MonsoonSheep.BusSchedules");
        if (busSchedules != null)
        {
            this.spawners.Add(new BusSpawner(busSchedules));
        }

        ModEntry.Events.GameLoop.DayStarted += this.OnDayStarted;
        ModEntry.Events.GameLoop.TimeChanged += this.OnTimeChanged;
    }

    private void OnReturnedToTitle(object? sender, ReturnedToTitleEventArgs e)
    {
        ModEntry.Events.GameLoop.DayStarted -= this.OnDayStarted;
        ModEntry.Events.GameLoop.TimeChanged -= this.OnTimeChanged;
    }

    private void OnDayStarted(object? sender, DayStartedEventArgs e)
    {
        this.visitSchedule.Clear();
        List<ActivityModel> activitiesForToday = this.activities.GetActivitiesForToday();
        Log.Debug("Activities today:");
        foreach (ActivityModel activity in activitiesForToday)
        {
            int[] timeRange = activity.TimeRange;

            int timeSelected = Utility.ConvertMinutesToTime(
                (Game1.random.Next(
                    Utility.ConvertTimeToMinutes(timeRange[0]),
                    Utility.ConvertTimeToMinutes(timeRange[1])
                    ) / 10) * 10
            );

            if (!this.visitSchedule.ContainsKey(timeSelected) || this.visitSchedule[timeSelected] == null)
                this.visitSchedule[timeSelected] = [];

            List<string> arriveBy = activity.ArriveBy;
            List<ISpawner?> spawners = arriveBy.Select(s => this.GetSpawner(s)).Where(s => s != null && s.IsAvailable()).ToList();
            ISpawner? spawner = spawners.MinBy(_ => Game1.random.Next());

            if (spawner == null)
            {
                Log.Warn($"Activity {activity.Id} doesn't have a valid ArriveBy: {activity.ArriveBy}");
                continue;
            }

            int durationMinutes = (activity.Duration) switch
            {
                "Instant" => 10,
                "Short" => 60,
                "Medium" => 150,
                "Long" => 350,
                _ => 60
            };

            Visit visit = new Visit(
                activity,
                spawner,
                startTime: timeSelected,
                endTime: Utility.ModifyTime(timeSelected, durationMinutes));

            this.visitSchedule[timeSelected].Add(visit);
            Log.Debug($"- {activity.Id} at {timeSelected} by {spawner.Id}");
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
                    this.PopulateNpcsForVisit(visit);

                    if (this.StartVisit(visit))
                    {
                        visit.state = VisitState.HeadingToDestination;
                    }
                    else
                    {
                        visit.state = VisitState.Failed;
                    }
                }
            }
        }

        // End visits that are due to end
        foreach (Visit visit in this.OngoingVisits())
        {
            if (Game1.timeOfDay >= visit.endTime)
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

                    continue;
                }

                visit.state = VisitState.HeadingBack;

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
    }

    private ISpawner? GetSpawner(string name)
        => this.spawners.FirstOrDefault(s => s.Id.Equals(name));

    private IEnumerable<Visit> OngoingVisits()
        => this.visitSchedule.Values.SelectMany(i => i).Where(v => v.state == VisitState.Ongoing);

    private void PopulateNpcsForVisit(Visit visit)
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
        // Spawn
        if (!visit.spawner.StartVisit(visit))
        {
            visit.state = VisitState.Failed;
            Log.Error($"Visit failed: {visit.activity.Id}, spawner {visit.spawner.Id}");
            foreach (NPC npc in visit.group)
            {
                npc.currentLocation?.characters.Remove(npc);
                npc.currentLocation = null;
                Utility.ForEachLocation((loc) => loc.characters.Remove(npc));
            }
            return false;
        }

        // Set behavior for activity when reached
        for (int i = 0; i < visit.group.Count; i++)
        {
            NPC npc = visit.group[i];
            string behavior = visit.activity.Actors[i].Behavior;
            
            npc.controller.endBehaviorFunction = delegate (Character c, GameLocation loc)
            {
                if (c is NPC n)
                {
                    n.StartActivityRouteEndBehavior(behavior, null);
                    visit.state = VisitState.Ongoing;
                }
            };   
        }

        return true;
    }
}
