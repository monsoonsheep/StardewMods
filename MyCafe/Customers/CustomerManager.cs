using MyCafe.Locations;
using MyCafe.CustomerFactory;
using StardewModdingAPI;
using System.Collections.Generic;
using System.Linq;

namespace MyCafe.Customers;

internal sealed class CustomerManager
{
    internal CustomerSpawner BusCustomers;
    internal CustomerSpawner VillagerCustomers;
    internal CustomerSpawner ChatCustomers;

    internal IEnumerable<CustomerGroup> CurrentGroups
    {
        get
        {
            var l = BusCustomers.ActiveGroups.Concat(VillagerCustomers.ActiveGroups);
            return ChatCustomers == null ? l : l.Concat(ChatCustomers.ActiveGroups);
        }
    }

    internal CustomerManager(IModHelper helper)
    {
        BusCustomers = new BusCustomerSpawner();
        VillagerCustomers = new VillagerCustomerSpawner();

        BusCustomers.Initialize(helper);
        VillagerCustomers.Initialize(helper);

#if YOUTUBE || TWITCH
        ChatCustomers = new ChatCustomerSpawner();
#endif
    }

    internal void DayUpdate()
    {
        VillagerCustomers.DayUpdate();
        BusCustomers.DayUpdate();
        ChatCustomers?.DayUpdate();
    }

    internal void SpawnCustomers()
    {
        Table table = Mod.Cafe.Tables.Where(t => !t.IsReserved).MinBy(t => t.Seats.Count);
        if (table == null)
        {
            Log.Debug("No tables available");
            return;
        }

        if (ChatCustomers is { State: SpawnerState.Enabled })
        {
            if (ChatCustomers.Spawn(table, out _) is true)
            {
                return;
            }
        }

        if (BusCustomers.Spawn(table, out _) is true)
        {
            return;
        }
    }

    internal void RemoveAllCustomers()
    {
        BusCustomers.RemoveAll();
        VillagerCustomers.RemoveAll();
        ChatCustomers?.RemoveAll();
    }

    internal CustomerGroup GetGroupFromTable(Table table)
    {
        return CurrentGroups.FirstOrDefault(g => g.ReservedTable == table);
    }

    internal void LetGo(CustomerGroup group, bool force = false)
    {
        if (BusCustomers.ActiveGroups.Contains(group))
            BusCustomers.LetGo(group, force);
        else if (VillagerCustomers.ActiveGroups.Contains(group))
            VillagerCustomers.LetGo(group, force);
        else if (ChatCustomers?.ActiveGroups.Contains(group) ?? false)
            ChatCustomers.LetGo(group, force);
        else
        {
            Log.Error("Group not found, couldn't be deleted. This is a bug.");
        }
    }
}
