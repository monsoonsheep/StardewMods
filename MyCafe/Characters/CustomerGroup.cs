using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using MonsoonSheep.Stardew.Common;
using MyCafe.Locations.Objects;
using StardewValley;
using StardewValley.Pathfinding;

namespace MyCafe.Characters;

public class CustomerGroup
{
    internal List<Customer> Members = [];
    internal Table? ReservedTable;

    internal CustomerGroup(List<Customer> members)
    {
        foreach (var m in members) this.Members.Add(m);
    }

    internal CustomerGroup()
    {
    }

    internal void Add(Customer customer)
    {
        customer.Group = this;
        this.Members.Add(customer);
    }

    internal bool ReserveTable(Table table)
    {
        if (table.Reserve(this.Members))
        {
            this.ReservedTable = table;
            return true;
        }

        return false;
    }

    internal bool MoveToTable()
    {
        List<Point> tiles = this.Members.Select(m => m.ReservedSeat!.Position).ToList();
        if (this.ReservedTable == null)
            return false;

        return this.MoveTo(CommonHelper.GetLocation(this.ReservedTable.CurrentLocation)!, tiles, Customer.SitDownBehavior);
    }

    internal bool MoveTo(GameLocation location, List<Point> tilePositions, PathFindController.endBehavior endBehavior)
    {
        for (int i = 0; i < this.Members.Count; i++)
        {
            if (!this.Members[i].PathTo(location, tilePositions[i], 3, endBehavior))
            {
                return false;
            }
        }

        return true;
    }

    internal bool MoveTo(GameLocation location, Point tile, PathFindController.endBehavior endBehavior)
    {
        List<Point> tiles = this.Members.Select(_ => tile).ToList();
        return this.MoveTo(location, tiles, endBehavior);
    }

    internal void Delete()
    {
        foreach (var c in this.Members)
        {
            c.currentLocation.characters.Remove(c);
        }
    }
}
