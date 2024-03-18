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
using StardewModdingAPI;
using StardewValley;
using SUtility = StardewValley.Utility;

namespace MyCafe.Characters.Spawning;
internal class VillagerCustomerSpawner : CustomerSpawner
{
    internal readonly Dictionary<string, VillagerCustomerData> VillagerData = new();

    internal override void Initialize(IModHelper helper)
    {
        foreach (var pair in Mod.Assets.VillagerVisitors)
        {
            this.VillagerData[pair.Key] = new VillagerCustomerData(pair.Value);
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

        List<NPC> npcs = data.Select(d => d.Npc).ToList();

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
            c.eventActor = true;
            c.ignoreScheduleToday = true;
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
                c.ReturnToSchedule();

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
        try
        {
            group.MoveTo(Game1.getLocationFromName("BusStop"), new Point(1, 23), (c, loc) => (c as NPC)!.ReturnToSchedule());
        }
        catch (PathNotFoundException e)
        {
            Log.Trace($"Villager NPCs can't find path out of cafe\n{e.Message}\n{e.StackTrace}");
            foreach (NPC npc in group.Members)
            {
                // TODO warp to their home
            }
        }

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
        NPC npc = data.Npc;

        int daysSinceLastVisit = Game1.Date.TotalDays - data.LastVisitedDate.TotalDays;
        int daysBetweenVisits = data.Model.VisitFrequency switch
        {
            0 => 200,
            1 => 28,
            2 => 15,
            3 => 8,
            4 => 2,
            5 => 0,
            _ => 9999999
        };

        foreach (CustomerGroup group in this._groups)
            if (group.Members.Contains(npc))
                return false;

        if (npc.isSleeping.Value is true ||
            npc.ScheduleKey == null ||
            daysSinceLastVisit <= daysBetweenVisits ||
            data.LastVisitedDate == Game1.Date)
            return false;

        // If no busy period for today, they're free all day
        if (!data.Model.BusyTimes.TryGetValue(npc.ScheduleKey, out List<BusyPeriod>? busyPeriods))
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

 
}
