using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using MonsoonSheep.Stardew.Common;
using MyCafe.Enums;
using MyCafe.Game;
using MyCafe.Locations.Objects;
using StardewValley;
using StardewValley.Pathfinding;

namespace MyCafe.Characters;

public class CustomerGroup
{
    internal GroupType Type;

    internal int MinutesSitting = 0;

    internal List<NPC> Members = [];

    internal Table? ReservedTable { get; set; }

    internal CustomerGroup(GroupType type)
    {
        this.Type = type;
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

    internal List<Seat> GetSeats()
    {
        return this.Members.Select(m => m.get_Seat()!).ToList();
    }

    internal void GoToTable()
    {
        List<Seat> seats = this.GetSeats();
        List<Point> tiles = seats.Select(s => new Point(s.TilePosition.X, s.TilePosition.Y)).ToList();
        this.MoveTo(CommonHelper.GetLocation(this.ReservedTable!.Location)!, tiles, NpcExtensions.SitDownBehavior);
    }

    internal void MoveTo(GameLocation location, Point tile, PathFindController.endBehavior endBehavior)
    {
        List<Point> tiles = this.Members.Select(_ => tile).ToList();
        this.MoveTo(location, tiles, endBehavior);
    }

    internal void MoveTo(GameLocation location, List<Point> tilePositions, PathFindController.endBehavior endBehavior)
    {
        for (int i = 0; i < this.Members.Count; i++)
        {
            this.Members[i].PathTo(location, tilePositions[i], 3, endBehavior);
        }
    }
}
