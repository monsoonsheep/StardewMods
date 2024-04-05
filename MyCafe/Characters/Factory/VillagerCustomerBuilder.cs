using System;
using System.Collections.Generic;
using System.Linq;
using MyCafe.Data.Customers;
using MyCafe.Enums;
using MyCafe.Netcode;
using StardewValley;

namespace MyCafe.Characters.Factory;

internal class VillagerCustomerBuilder : CustomerBuilder
{
    private List<VillagerCustomerData> npcVisitData = [];

    internal VillagerCustomerBuilder(Func<NPC, Item?> menuItemSelector) : base(menuItemSelector)
    {
    }

    internal override CustomerGroup? BuildGroup()
    {
        this.npcVisitData = Mod.Cafe.GetAvailableVillagerCustomers(1);
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
            c.ignoreScheduleToday = true;
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
}