using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using System.Collections.Generic;
using System.Linq;
using StardewValley.Pathfinding;
using MyCafe.ChairsAndTables;

namespace MyCafe.Customers;

internal class CustomerGroup
{
    internal List<Customer> Members;
    internal Table ReservedTable;

    internal CustomerGroup(List<Customer> members)
    {
        Members = members;
    }

    internal CustomerGroup()
    {
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
        foreach (Customer c in Members)
            c.State = Customer.CustomerState.GoingToTable;
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