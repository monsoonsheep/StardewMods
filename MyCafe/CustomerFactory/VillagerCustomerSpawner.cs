using Microsoft.Xna.Framework;
using MyCafe.Locations;
using MyCafe.Customers;
using MyCafe.Customers.Data;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Pathfinding;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyCafe.CustomerFactory;

internal class VillagerCustomerSpawner : CustomerSpawner
{
    internal readonly Dictionary<string, VillagerCustomerData> VillagerData = new();

    internal override Task<bool> Initialize(IModHelper helper)
    {
        int count = 0, doneCount = 0;
        SUtility.ForEachVillager(npc =>
        {
            try
            {
                if (Game1.content.Load<VillagerCustomerData>(ModKeys.ASSETS_NPCSCHEDULE_PREFIX + npc.Name) is { } data)
                {
                    VillagerData[npc.Name] = data;
                    doneCount++;
                }
            }
            catch
            {
                // ignored
            }

            count++;
            return true;
        });

        Log.Debug($"{doneCount} NPCs have Schedule Data. The other {count} won't visit the cafe.");
        return Task.FromResult(true);
    }

    private bool CanNpcVisitDuringTime(NPC npc, int timeOfDay)
    {
        VillagerCustomerData data = VillagerData[npc.Name];

        if (data == null ||
            npc.isSleeping.Value is true ||
            npc.ScheduleKey == null ||
            data.CanVisitToday == false ||
            data.LastVisitedDate == Game1.Date)
            return false;

        // If no busy period for today, they're free all day
        if (!data.BusyTimes.TryGetValue(npc.ScheduleKey, out List<BusyPeriod> busyPeriods) || busyPeriods.Count == 0)
            return true;

        // Check their busy periods for their current schedule key
        foreach (BusyPeriod busyPeriod in busyPeriods)
        {
            if (SUtility.CalculateMinutesBetweenTimes(timeOfDay, busyPeriod.From) <= 120
                && SUtility.CalculateMinutesBetweenTimes(timeOfDay, busyPeriod.To) > 0)
            {
                if (!(busyPeriod.Priority <= 3 && Game1.random.Next(6 * busyPeriod.Priority) == 0) &&
                    !(busyPeriod.Priority == 4 && Game1.random.Next(50) == 0))
                {
                    return false;
                }
            }
        }

        return true;
    }

    internal override bool Spawn(Table table, out CustomerGroup groupSpawned)
    {
        NPC npc = Game1.getCharacterFromName("Shane");

        Customer customer = new Customer(npc.Name, npc.Position, npc.DefaultMap, npc.Sprite, npc.Portrait);
        npc.currentLocation.characters.Remove(npc);
        npc.currentLocation.characters.Add(customer);

        VillagerData[npc.Name].RealNpc = npc;
        CustomerGroup group = new CustomerGroup([customer]);
        ActiveGroups.Add(group);
        groupSpawned = group;
        return true;
    }

    internal override bool LetGo(CustomerGroup group, bool force = false)
    {
        if (!base.LetGo(group))
            return false;

        Customer v = group.Members.First();
        NPC original = VillagerData[v.Name].RealNpc;

        if (original != null)
        {
            original.currentLocation = v.currentLocation;
            original.Position = v.Position;
            original.TryLoadSchedule(v.ScheduleKey);
            original.faceDirection(v.FacingDirection);
            original.ignoreScheduleToday = false;

            var activityTimes = v.Schedule.Keys.OrderBy(i => i).ToList();
            int timeOfCurrent = activityTimes.LastOrDefault(t => t <= Game1.timeOfDay);
            int timeOfNext = activityTimes.FirstOrDefault(t => t > Game1.timeOfDay);
            var minutesSinceCurrentStarted = SUtility.CalculateMinutesBetweenTimes(timeOfCurrent, Game1.timeOfDay);
            var minutesTillNextStarts = SUtility.CalculateMinutesBetweenTimes(Game1.timeOfDay, timeOfNext);
            int timeOfActivity;
            if (timeOfCurrent == 0) // Means it's the start of the day
            {
                timeOfActivity = activityTimes.First();
            }
            else if (timeOfNext == 0) // Means it's the end of the day
            {
                timeOfActivity = activityTimes.Last();
            }
            else
            {
                if (minutesTillNextStarts < minutesSinceCurrentStarted && minutesTillNextStarts <= 30)
                    // If we're very close to the next item, 
                    timeOfActivity = timeOfNext;
                else
                    timeOfActivity = timeOfCurrent;

            }

            SchedulePathDescription originalPathDescription = original.Schedule[timeOfActivity];

            GameLocation targetLocation = Game1.getLocationFromName(originalPathDescription.targetLocationName);
            if (targetLocation != null)
            {
                Stack<Point> routeToScheduleItem =
                    PathfindingExtensions.PathfindFromLocationToLocation(original.currentLocation, original.TilePoint,
                                                               targetLocation, originalPathDescription.targetTile,
                                                               original);

                SchedulePathDescription toInsert = new SchedulePathDescription(
                    routeToScheduleItem,
                    originalPathDescription.facingDirection,
                    originalPathDescription.endOfRouteBehavior,
                    originalPathDescription.endOfRouteMessage,
                    targetLocation.Name,
                    originalPathDescription.targetTile)
                {
                    time = Game1.timeOfDay
                };

                original.queuedSchedulePaths.Clear();
                original.Schedule[Game1.timeOfDay] = toInsert;
                original.checkSchedule(Game1.timeOfDay);
            }
        }

        v.currentLocation.characters.Remove(v);
        v.currentLocation.addCharacter(original);
        return true;
    }

    internal override void DayUpdate()
    {
        // Set which NPCs can visit today based on how many days it's been since their last visit, and their 
        // visit frequency level given in their visit data.
        foreach (var data in VillagerData)
        {
            int daysSinceLastVisit = Game1.Date.TotalDays - data.Value.LastVisitedDate.TotalDays;
            int daysAllowedBetweenVisits = data.Value.Frequency switch
            {
                0 => 200,
                1 => 28,
                2 => 15,
                3 => 8,
                4 => 2,
                5 => 0,
                _ => 9999999
            };

            data.Value.CanVisitToday = daysSinceLastVisit > daysAllowedBetweenVisits;
        }
    }
}