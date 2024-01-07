using Microsoft.Xna.Framework.Graphics;
using MyCafe.Framework.ChairsAndTables;
using MyCafe.Framework.Customers;
using StardewValley;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using SUtility = StardewValley.Utility;
using StardewValley.Buildings;

namespace MyCafe.Framework.Managers;

internal sealed class CustomerManager
{
    internal static CustomerManager Instance;

    internal BusCustomerSpawner BusCustomers;
    internal VillagerCustomerSpawner VillagerCustomers;
    internal readonly List<CustomerGroup> CurrentGroups = new();




    internal IEnumerable<Customer> CurrentCustomers
        => CurrentGroups.SelectMany(g => g.Members);

    internal CustomerManager()
    {
        Instance = this;
        BusCustomers = new BusCustomerSpawner();
        VillagerCustomers = new VillagerCustomerSpawner();
    }

    internal void SpawnBusCustomers()
    {
        Table table = Mod.Tables.CurrentTables.Where(t => !t.IsReadyToOrder).MinBy(t => t.Seats.Count);
        if (table == null)
        {
            Log.Debug("No tables available");
            return;
        }
        BusCustomers.Spawn(table, out CustomerGroup group);
        CurrentGroups.Add((group));
    }

    internal void RemoveAllCustomers()
    {

    }
}
