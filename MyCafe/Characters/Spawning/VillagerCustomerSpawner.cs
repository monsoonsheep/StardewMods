using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using MyCafe.Data.Customers;
using MyCafe.Data.Models;
using MyCafe.Enums;
using MyCafe.Locations.Objects;
using MyCafe.Netcode;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Pathfinding;
using SUtility = StardewValley.Utility;

namespace MyCafe.Characters.Spawning;
internal class VillagerCustomerSpawner : CustomerSpawner
{
    internal readonly Dictionary<string, VillagerCustomerData> VillagerData = new();

    internal override void Initialize(IModHelper helper)
    {
        foreach (var model in Mod.Assets.VillagerCustomerModels)
        {
            VillagerCustomerData data = new VillagerCustomerData(model.Value.NpcName);
            this.VillagerData[model.Key] = data;
        }
    }

    internal override void DayUpdate()
    {
    }

    internal override bool Spawn(Table table)
    {
        List<VillagerCustomerData> data = this.GetAvailableVillagerCustomers(1);
        if (data.Count == 0)
        {
            Log.Debug("No villager customers can be created");
            return false;
        }

        List<NPC> npcs = data.Select(d => d.GetNpc()).ToList();

        CustomerGroup group = new CustomerGroup(GroupType.Villager, this);
        foreach (NPC npc in npcs)
            group.AddMember(npc);

        if (group.ReserveTable(table) == false)
        {
            return false;
        }

        foreach (NPC c in group.Members)
        {
            c.get_OrderItem().Set(Debug.SetTestItemForOrder(c));
            //c.eventActor = true; // Not doing eventactor, it's a workaround for the NPCBarrier tile property, but we're removing that property now
            c.ignoreScheduleToday = true;
            Mod.Cafe.NpcCustomers.Add(c.Name);
            Log.Trace($"{c.Name} is coming.");
        }

        try
        {
            group.GoToTable();
        }
        catch (PathNotFoundException e)
        {
            Log.Error($"Couldn't make villager customers. Reverting changes...\n{e.Message}\n{e.StackTrace}");

            foreach (NPC c in group.Members)
                ReturnToSchedule(c);

            return false;
        }

        foreach (VillagerCustomerData d in data)
        {
            d.LastVisitedDate = Game1.Date;
        }

        this._groups.Add(group);
        return true;
    }

    internal override bool EndCustomers(CustomerGroup group, bool force = false)
    {
        group.ReservedTable?.Free();
        try
        {
            group.MoveTo(Game1.getLocationFromName("BusStop"), new Point(12, 23), (c, _) => ReturnToSchedule((c as NPC)!));
        }
        catch (PathNotFoundException e)
        {
            Log.Error($"Villager NPCs can't find path out of cafe\n{e.Message}\n{e.StackTrace}");
            foreach (NPC npc in group.Members)
            {
                // TODO warp to their home
            }
        }

        this._groups.Remove(group);

        return true;
    }

    private List<VillagerCustomerData> GetAvailableVillagerCustomers(int count)
    {
        List<VillagerCustomerData> list = [];

        foreach (KeyValuePair<string, VillagerCustomerData> data in this.VillagerData.OrderBy(_ => Game1.random.Next()))
        {
            if (list.Count == count)
                break;

            if (this.CanVillagerVisit(data.Value, Game1.timeOfDay))
                list.Add(data.Value);
        }

        return list;
    }

    private bool CanVillagerVisit(VillagerCustomerData data, int timeOfDay)
    {
        NPC npc = data.GetNpc();
        VillagerCustomerModel model = Mod.Assets.VillagerCustomerModels[data.NpcName];

        int daysSinceLastVisit = Game1.Date.TotalDays - data.LastVisitedDate.TotalDays;
        int daysAllowed = model.VisitFrequency switch
        {
            0 => 200,
            1 => 27,
            2 => 13,
            3 => 7,
            4 => 3,
            5 => 1,
            _ => 9999999
        };

        if (Mod.Cafe.NpcCustomers.Contains(data.NpcName) ||
            npc.isSleeping.Value == true ||
            npc.ScheduleKey == null ||
            daysSinceLastVisit < daysAllowed)
            return false;

        // If no busy period for today, they're free all day
        if (!model.BusyTimes.TryGetValue(npc.ScheduleKey, out List<BusyPeriod>? busyPeriods))
            return false;
        if (busyPeriods.Count == 0)
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

    internal static void ReturnToSchedule(NPC npc)
    {
        Mod.Cafe.NpcCustomers.Remove(npc.Name);
        npc.ignoreScheduleToday = false;

        List<int> activityTimes = npc.Schedule.Keys.OrderBy(i => i).ToList();
        int timeOfCurrent = activityTimes.LastOrDefault(t => t <= Game1.timeOfDay);
        int timeOfNext = activityTimes.FirstOrDefault(t => t > Game1.timeOfDay);
        int minutesSinceCurrentStarted = SUtility.CalculateMinutesBetweenTimes(timeOfCurrent, Game1.timeOfDay);
        int minutesTillNextStarts = SUtility.CalculateMinutesBetweenTimes(Game1.timeOfDay, timeOfNext);
        int timeOfActivity;

        Log.Trace($"[{Game1.timeOfDay}] Returning {npc.Name} to schedule for key \"{npc.ScheduleKey}\". Time of current activity is {timeOfCurrent}, next activity is at {timeOfNext}.");

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

        Log.Trace($"Time of selected activity is {timeOfActivity}");

        SchedulePathDescription originalPathDescription = npc.Schedule[timeOfActivity];

        Log.Trace($"Schedule description is {originalPathDescription.targetLocationName}: {originalPathDescription.targetTile}, behavior: {originalPathDescription.endOfRouteBehavior}");

        GameLocation targetLocation = Game1.getLocationFromName(originalPathDescription.targetLocationName);
        Stack<Point>? routeToScheduleItem = Pathfinding.PathfindFromLocationToLocation(
            npc.currentLocation,
            npc.TilePoint,
            targetLocation,
            originalPathDescription.targetTile,
            npc);

        if (routeToScheduleItem == null)
        {
            Log.Trace("Can't find route back");
            // TODO: Warp them to their home
            return;
        }

        // Can this return null?
        SchedulePathDescription toInsert = npc.pathfindToNextScheduleLocation(
            npc.ScheduleKey,
            npc.currentLocation.Name,
            npc.TilePoint.X,
            npc.TilePoint.Y,
            originalPathDescription.targetLocationName,
            originalPathDescription.targetTile.X,
            originalPathDescription.targetTile.Y,
            originalPathDescription.facingDirection,
            originalPathDescription.endOfRouteBehavior,
            originalPathDescription.endOfRouteMessage);

        npc.queuedSchedulePaths.Clear();
        npc.Schedule[Game1.timeOfDay] = toInsert;
        npc.checkSchedule(Game1.timeOfDay);
    }
}
