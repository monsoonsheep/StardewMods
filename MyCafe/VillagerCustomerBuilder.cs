using System.Collections.Generic;
using System.Linq;
using MyCafe.Characters;
using MyCafe.Data.Customers;
using MyCafe.Enums;
using MyCafe.Locations.Objects;
using MyCafe.Netcode;
using StardewValley;

#nullable disable

namespace MyCafe;

internal class VillagerCustomerBuilder : CustomerBuilder
{
    private List<VillagerCustomerData> npcVisitData = [];

    internal override CustomerGroup BuildGroup()
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

    internal override bool SetupGroup()
    {
        foreach (NPC c in this._group.Members)
        {
            c.get_OrderItem().Set(Debug.SetTestItemForOrder(c));
        }

        return true;
    }

    internal override bool PreMove()
    {
        foreach (NPC c in this._group.Members)
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
            this._group.GoToTable();
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
        foreach (NPC c in this._group.Members)
        {
            Mod.Cafe.NpcCustomers.Add(c.Name);
        }

        foreach (var data in this.npcVisitData)
        {
            data.LastVisitedDate = Game1.Date;
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
}
