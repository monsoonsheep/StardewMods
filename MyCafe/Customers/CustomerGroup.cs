using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Pathfinding;
using System.Collections.Generic;
using System.Linq;
using MyCafe.Locations.Objects;
using MonsoonSheep.Stardew.Common;

namespace MyCafe.Customers;

public class CustomerGroup
{
    internal List<Customer> Members = [];
    internal Table? ReservedTable;

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
        List<Point> tiles = Members.Select(m => m.ReservedSeat!.Position).ToList();
        if (ReservedTable == null)
            return false;

        ReservedTable.State.Set(TableState.WaitingForCustomers);
        return MoveTo(CommonHelper.GetLocation(ReservedTable.CurrentLocation)!, tiles, Customer.SitDownBehavior);
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

    internal bool MoveTo(GameLocation location, Point tile, PathFindController.endBehavior endBehavior)
    {
        List<Point> tiles = Members.Select(_ => tile).ToList();
        return MoveTo(location, tiles, endBehavior);
    }

    internal void Delete()
    {
        foreach (var c in Members)
        {
            c.currentLocation.characters.Remove(c);
        }
    }
}