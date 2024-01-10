using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using SUtility = StardewValley.Utility;
using StardewValley.Buildings;
using MyCafe.ChairsAndTables;
using MyCafe.Customers;

namespace MyCafe.Managers;

internal sealed class CustomerManager
{
    internal static CustomerManager Instance;

    internal BusCustomerSpawner BusCustomers;
    internal VillagerCustomerSpawner VillagerCustomers;

    internal readonly List<CustomerGroup> CurrentGroups = new();

    internal IEnumerable<Customer> CurrentCustomers
        => CurrentGroups.SelectMany(g => g.Members);

    internal CustomerManager(IModHelper helper)
    {
        Instance = this;

        Mod.Assets.LoadStoredCustomerData();
        BusCustomers = new(helper);
        VillagerCustomers = new(helper);
    }

    internal void DayUpdate()
    {
        VillagerCustomers.DayUpdate();
        BusCustomers.DayUpdate();
    }

    internal void SpawnBusCustomers()
    {
        Table table = Mod.Cafe.Tables.Where(t => !t.IsReserved.Value).MinBy(t => t.Seats.Count);
        if (table == null)
        {
            Log.Debug("No tables available");
            return;
        }
        if (BusCustomers.Spawn(table, out CustomerGroup group) is true)
            CurrentGroups.Add(group);
    }

    internal void SpawnVillagerCustomers()
    {

    }

    internal void RemoveAllCustomers()
    {

    }
}
