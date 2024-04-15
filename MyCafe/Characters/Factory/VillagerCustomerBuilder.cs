using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Microsoft.Xna.Framework;
using MyCafe.Data.Customers;
using MyCafe.Data.Models;
using MyCafe.Enums;
using MyCafe.Locations.Objects;
using MyCafe.Netcode;
using Netcode;
using StardewValley;
using StardewValley.TerrainFeatures;
using SUtility = StardewValley.Utility;

namespace MyCafe.Characters.Factory;

internal class VillagerCustomerBuilder : CustomerBuilder
{
    internal override CustomerGroup? GenerateGroup()
    {
        List<VillagerCustomerData> data = GetAvailableVillagerCustomers(1);
        if (data.Count == 0)
        {
            Log.Debug("No villager customers can be created");
            return null;
        }

        List<NPC> npcs = data.Select(d => d.GetNpc()).ToList();

        CustomerGroup group = new CustomerGroup(GroupType.Villager);
        foreach (NPC npc in npcs)
            group.AddMember(npc);

        return group;
    }

    internal override bool PreMove()
    {
        foreach (NPC c in this._group!.Members)
        {
            Log.Info($"{c.Name} is coming.");
            Game1.addHUDMessage(new HUDMessage($"{c.Name} is coming."));
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

    private static List<VillagerCustomerData> GetAvailableVillagerCustomers(int count)
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
        int daysAllowed = model.VisitFrequency switch
        {
            1 => 27,
            2 => 13,
            3 => 7,
            4 => 3,
            5 => 1,
            _ => 9999999
        };

        #if DEBUG
        daysAllowed = 1;
        #endif

        if (Mod.Cafe.NpcCustomers.Contains(data.NpcName) ||
            npc.isSleeping.Value == true ||
            npc.ScheduleKey == null ||
            npc.controller != null ||
            daysSinceLastVisit < daysAllowed)
            return false;

        // If no busy period for today, they're free all day
        if (!model.BusyTimes.TryGetValue(npc.ScheduleKey, out List<BusyPeriod>? busyPeriods))
            return false;
        if (busyPeriods.Count == 0)
            return true;

        #if DEBUG
        return true;
        #endif

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
