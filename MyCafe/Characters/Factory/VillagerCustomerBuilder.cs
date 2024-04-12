using System;
using System.Collections.Generic;
using System.Linq;
using MyCafe.Data.Customers;
using MyCafe.Data.Models;
using MyCafe.Enums;
using MyCafe.Netcode;
using StardewValley;
using SUtility = StardewValley.Utility;

namespace MyCafe.Characters.Factory;

internal class VillagerCustomerBuilder : CustomerBuilder
{
    private List<VillagerCustomerData> npcVisitData = [];

    internal VillagerCustomerBuilder(Func<NPC, Item?> menuItemSelector) : base(menuItemSelector)
    {
    }

    internal override CustomerGroup? BuildGroup()
    {
        this.npcVisitData = GetAvailableVillagerCustomers(1);
        if (this.npcVisitData.Count == 0)
        {
            Log.Debug("No villager customers can be created");
            return null;
        }

        List<NPC> npcs = this.npcVisitData.Select(d => d.GetNpc()).ToList();

        CustomerGroup group = new CustomerGroup(GroupType.Villager);
        foreach (NPC npc in npcs)
            group.AddMember(npc);

        return group;
    }

    internal override bool PreMove()
    {
        foreach (NPC c in this._group!.Members)
        {
            Log.Trace($"{c.Name} is coming.");
        }

        return true;
    }

    internal override bool MoveToTable()
    {
        try
        {
            this._group!.GoToTable();
        }
        catch (PathNotFoundException e)
        {
            Log.Error($"Couldn't make villager customers. Reverting changes...\n{e.Message}\n{e.StackTrace}");
            return false;
        }

        return true;
    }

    internal override bool PostMove()
    {
        foreach (NPC c in this._group!.Members)
        {
            c.ignoreScheduleToday = true;
            Mod.Cafe.NpcCustomers.Add(c.Name);
        }

        return true;
    }

    internal override void RevertChanges()
    {
        if (this._group == null)
            return;

        foreach (NPC c in this._group.Members)
        {
            Mod.Cafe.ReturnVillagerToSchedule(c);
        }
    }

    internal static List<VillagerCustomerData> GetAvailableVillagerCustomers(int count)
    {
        List<VillagerCustomerData> list = [];

        foreach (KeyValuePair<string, VillagerCustomerData> data in Mod.Instance.VillagerData.OrderBy(_ => Game1.random.Next()))
        {
            if (list.Count == count)
                break;

            if (CanVillagerVisit(data.Value, Game1.timeOfDay))
                list.Add(data.Value);
        }

        return list;
    }

    private static bool CanVillagerVisit(VillagerCustomerData data, int timeOfDay)
    {
        NPC npc = data.GetNpc();
        VillagerCustomerModel model = Mod.Instance.VillagerCustomerModels[data.NpcName];

        int daysSinceLastVisit = Game1.Date.TotalDays - data.LastVisitedDate.TotalDays;
        int daysAllowed = Debug.IsDebug() ? 1 : model.VisitFrequency switch
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
        if (Debug.IsDebug() || busyPeriods.Count == 0)
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
