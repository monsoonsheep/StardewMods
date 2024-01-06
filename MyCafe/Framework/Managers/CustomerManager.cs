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

    internal readonly List<CustomerGroup> CurrentGroups = new();

    internal IEnumerable<Customer> CurrentCustomers
        => CurrentGroups.SelectMany(g => g.Members);

    internal CustomerManager()
    {
        Instance = this;
    }

    internal void RemoveAllCustomers()
    {

    }
}
