using MyCafe.ChairsAndTables;
using MyCafe.CustomerProduction;
using MyCafe.Customers;
using StardewModdingAPI;
using System.Collections.Generic;
using System.Linq;

namespace MyCafe;

internal sealed partial class CustomerManager
{
    internal ICustomerSpawner BusCustomers;
    internal ICustomerSpawner VillagerCustomers;
    internal ICustomerSpawner ChatCustomers;

    internal readonly List<CustomerGroup> CurrentGroups = new();

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
        ChatCustomers.DayUpdate();
    }

    internal void SpawnCustomers()
    {
        Table table = Mod.Cafe.Tables.Where(t => !t.IsReserved).MinBy(t => t.Seats.Count);
        if (table == null)
        {
            Log.Debug("No tables available");
            return;
        }

        CustomerGroup group;

        if (ChatCustomers != null)
        {
            if (ChatCustomers.Spawn(table, out group) is true)
            {
                CurrentGroups.Add(group);
                return;
            }
        }

        if (BusCustomers.Spawn(table, out group) is true)
        {
            CurrentGroups.Add(group);
            return;
        }
    }

    internal void RemoveAllCustomers()
    {

    }
}
