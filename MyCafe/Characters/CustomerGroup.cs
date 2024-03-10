using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using MonsoonSheep.Stardew.Common;
using MyCafe.Enums;
using MyCafe.Locations.Objects;
using StardewValley;
using StardewValley.Pathfinding;

namespace MyCafe.Characters;

public class CustomerGroup
{
    internal GroupType Type;
    internal List<NPC> Members = [];
    internal Table? ReservedTable { get; set; }

    internal CustomerGroup(List<NPC> members)
    {
        foreach (var m in members) this.AddMember(m);
    }

    internal void AddMember(NPC member)
    {
        member.set_Group(this);
        this.Members.Add(member);
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

    internal List<Seat>? GetSeats()
    {
        return this.Members.Any(m => m.get_Seat() == null) ? null : this.Members.Select(m => m.get_Seat()!).ToList();
    }

    internal bool MoveToTable()
    {
        List<Seat>? seats = this.GetSeats();
        if (seats == null || this.ReservedTable == null)
            return false;

        List<Point> tiles = seats.Select(s => s!.Position).ToList();
        return this.MoveTo(CommonHelper.GetLocation(this.ReservedTable.CurrentLocation)!, tiles, NpcExtensions.SitDownBehavior);
    }

    internal bool MoveTo(GameLocation location, Point tile, PathFindController.endBehavior endBehavior)
    {
        List<Point> tiles = this.Members.Select(_ => tile).ToList();
        return this.MoveTo(location, tiles, endBehavior);
    }

    internal bool MoveTo(GameLocation location, List<Point> tilePositions, PathFindController.endBehavior endBehavior)
    {
        for (int i = 0; i < this.Members.Count; i++)
        {
            NPC member = this.Members[i];
            if (!member.PathTo(location, tilePositions[i], 3, endBehavior))
                return false;

            if (member.get_IsSittingDown())
            {
                member.set_IsSittingDown(false);
                int direction = CommonHelper.DirectionIntFromVectors(member.Tile, member.controller.pathToEndPoint.First().ToVector2());
                if (direction != -1)
                {
                    member.Jump(direction);
                    member.Freeze();
                    member.set_AfterLerp(c => c.Unfreeze());
                }
            }
        }

        return true;
    }

    internal void StartEating()
    {

    }

    internal void Delete()
    {
        foreach (var c in this.Members)
        {
            c.currentLocation.characters.Remove(c);
        }
    }
}
