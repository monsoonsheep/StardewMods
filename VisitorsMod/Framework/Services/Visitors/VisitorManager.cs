using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewMods.VisitorsMod.Framework.Models;
using StardewMods.VisitorsMod.Framework.Models.Activities;
using System.Text.RegularExpressions;

using StardewValley.Pathfinding;
using StardewValley;
using Microsoft.CodeAnalysis.Text;
using StardewMods.VisitorsMod.Framework.Visitors;
using StardewMods.VisitorsMod.Framework.Data;
using StardewMods.VisitorsMod.Framework.Enums;
using System.Diagnostics;
using StardewMods.VisitorsMod.Framework.Services.Visitors.Activities;
using HarmonyLib;
using static StardewValley.Menus.ConfirmationDialog;

namespace StardewMods.VisitorsMod.Framework.Services.Visitors;

internal class VisitorManager : Service
{
    private static VisitorManager instance = null!;

    private readonly ICollection<ISpawner> spawners;
    private readonly ActivityManager activities;
    private readonly RandomVisitorBuilder visitorBuilder;

    internal readonly Dictionary<string, VisitorModel> visitorModels = [];

    private readonly Dictionary<int, List<Visit>> visitSchedule = [];

    internal IEnumerable<Visit> OngoingVisits
        => this.visitSchedule.Values.SelectMany(i => i).Where(v => v.state == VisitState.Ongoing);

    public VisitorManager(
        ContentPacks contentPacks,
        ICollection<ISpawner> spawners,
        ActivityManager activities,
        RandomVisitorBuilder visitorBuilder,
        Harmony harmony,
        IModEvents events,
        ILogger logger,
        IManifest manifest)
        : base(logger, manifest)
    {
        instance = this;

        this.visitorModels = contentPacks.visitorModels;

        this.spawners = spawners;
        this.activities = activities;
        this.visitorBuilder = visitorBuilder;

        events.GameLoop.DayStarted += this.OnDayStarted;
        events.GameLoop.TimeChanged += this.OnTimeChanged;
    }

    private void OnDayStarted(object? sender, DayStartedEventArgs e)
    {
        this.visitSchedule.Clear();
        List<ActivityModel> activitiesForToday = this.activities.GetActivitiesForToday();
        this.Log.Debug("Activities today:");
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
                this.Log.Warn($"Activity {activity.Id} doesn't have a valid ArriveBy: {activity.ArriveBy}");
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
            this.Log.Debug($"- {activity.Id} at {timeSelected} by {spawner.Id}");
        }
    }

    private void OnTimeChanged(object? sender, TimeChangedEventArgs e)
    {
        if (!this.visitSchedule.ContainsKey(Game1.timeOfDay))
            return;

        foreach (Visit visit in this.visitSchedule[Game1.timeOfDay])
        {
            if (visit.state == VisitState.NotStarted)
            {
                this.PopulateNpcsForVisit(visit);

                if (this.StartVisit(visit))
                {
                    visit.state = VisitState.HeadingToDestination;
                }
            }
            else if (visit.state == VisitState.Ongoing)
            {
                if (Game1.timeOfDay >= visit.endTime)
                {
                    if (!visit.spawner.EndVisit(visit))
                    {
                        this.Log.Error("NPCs couldn't path back");
                        for (int i = 0; i < visit.group.Count; i++)
                        {
                            NPC npc = visit.group[i];
                            npc.currentLocation.characters.Remove(npc);
                            npc.currentLocation = null;
                            visit.state = VisitState.Ended;
                        }
                        continue;
                    }
                    
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
    }

    private void PopulateNpcsForVisit(Visit visit)
    {
        visit.group = [];

        // Create NPCs
        foreach (var actor in visit.activity.Actors)
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
            npc.portraitOverridden = true; // don't load portraits again

            visit.group.Add(npc);
        }
    }

    private bool StartVisit(Visit visit)
    {
        // Spawn
        if (!visit.spawner.StartVisit(visit))
        {
            visit.state = VisitState.Failed;
            this.Log.Error($"Visit failed: {visit.activity.Id}, spawner {visit.spawner.Id}");
            foreach (NPC npc in visit.group)
            {
                npc.currentLocation?.characters.Remove(npc);
                npc.currentLocation = null;
                Utility.ForEachLocation((loc) => loc.characters.Remove(npc));
            }
            return false;
        }

        // Set end behavior
        for (int i = 0; i < visit.group.Count; i++)
        {
            NPC npc = visit.group[i];
            string behavior = visit.activity.Actors[i].Behavior;
            npc.controller.endBehaviorFunction = (PathFindController.endBehavior?)
                AccessTools.Method(
                    typeof(NPC),
                    "getRouteEndBehaviorFunction",
                    [typeof(string), typeof(string)])
                .Invoke(npc, [behavior, null]);
        }

        return true;
    }

    private ISpawner? GetSpawner(string name)
        => this.spawners.FirstOrDefault(s => s.Id.Equals(name));

    internal void DebugSpawnTestNpc()
    {
        ActivityModel activity = this.activities.DebugGetActivity();
    }
}
