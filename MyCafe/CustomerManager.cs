using MyCafe.ChairsAndTables;
using MyCafe.CustomerFactory;
using MyCafe.Customers;
using StardewModdingAPI;
using System.Collections.Generic;
using System.Linq;

namespace MyCafe;

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
            return (ChatCustomers == null) ? l : l.Concat(ChatCustomers.ActiveGroups);
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
        ChatCustomers.Initialize(helper);
#endif
    }

    internal IEnumerable<Customer> CurrentCustomers
        => CurrentGroups.SelectMany(g => g.Members);

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

        if (ChatCustomers != null)
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

    }

    internal CustomerGroup GetGroupFromTable(Table table)
    {
        return CurrentGroups.FirstOrDefault(g => g.ReservedTable == table);
    }

    internal void ReleaseGroup(CustomerGroup group)
    {
        if (BusCustomers.ActiveGroups.Contains(group))
            BusCustomers.LetGo(group);
        else if (VillagerCustomers.ActiveGroups.Contains(group))
            VillagerCustomers.LetGo(group);
        else if (ChatCustomers?.ActiveGroups.Contains(group) ?? false)
            ChatCustomers.LetGo(group);
        else
        {
            Log.Error("Group not found");
        }
    }
}
