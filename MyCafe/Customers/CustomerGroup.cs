using Microsoft.Xna.Framework;
using MyCafe.ChairsAndTables;
using StardewValley;
using StardewValley.Pathfinding;
using System.Collections.Generic;
using System.Linq;

namespace MyCafe.Customers;

public class CustomerGroup
{
    internal List<Customer> Members = [];
    internal Table ReservedTable;

    internal CustomerGroup(List<Customer> members)
    {
        foreach (var m in members)
            Members.Add(m);
    }

    internal CustomerGroup()
    {
    }

    internal void Add(Customer customer)
    {
        customer.Group = this;
        Members.Add(customer);
    }

    internal bool ReserveTable(Table table)
    {
        if (table.Reserve(Members))
        {
            ReservedTable = table;
            return true;
        }

        return false;
    }

    internal bool MoveToTable()
    {
        List<Point> tiles = Members.Select(m => m.ReservedSeat.Value.Position).ToList();
        ReservedTable.State.Set(TableState.WaitingForCustomers);
        return MoveTo(Utility.GetLocationFromName(ReservedTable.CurrentLocation), tiles, Customer.SitDownBehavior);
    }

    internal bool MoveTo(GameLocation location, List<Point> tilePositions, PathFindController.endBehavior endBehavior)
    {
        for (var i = 0; i < Members.Count; i++)
        {
            if (!Members[i].PathTo(location, tilePositions[i], 3, endBehavior))
            {
                return false;
            }
        }

        return true;
    }

    internal void Delete()
    {
        foreach (Customer customer in Members)
        {
            customer.currentLocation.characters.Remove(customer);
        }
    }
}