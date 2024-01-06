using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using System.Collections.Generic;
using System.Linq;
using MyCafe.Framework.ChairsAndTables;
using StardewValley.Pathfinding;

namespace MyCafe.Framework.Customers;

internal class CustomerGroup
{
    internal List<Customer> Members;
    internal Table ReservedTable;

    internal CustomerGroup(List<Customer> members)
    {
        Members = members;
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
        List<Point> tiles = Members.Select(m => m.ReservedSeat.Position.ToPoint()).ToList();
        return MoveTo(Utility.GetLocationFromName(ReservedTable.CurrentLocation), tiles, Customer.SitDownBehavior);
    }

    internal bool MoveTo(GameLocation location, List<Point> tilePositions, PathFindController.endBehavior endBehavior)
    {
        bool success = true;
        for (var i = 0; i < Members.Count; i++)
        {
            if (!Members[i].PathTo(location, tilePositions[i], 3, endBehavior))
            {
                success = false;
                break;
            }
        }

        return success;
    }
}