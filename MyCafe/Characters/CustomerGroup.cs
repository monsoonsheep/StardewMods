using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using MonsoonSheep.Stardew.Common;
using MyCafe.Characters.Spawning;
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

    internal CustomerSpawner _spawner;

    internal CustomerGroup(GroupType type, CustomerSpawner spawner)
    {
        this._spawner = spawner;
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
        List<Point> tiles = seats.Select(s => s!.Position).ToList();
        this.MoveTo(CommonHelper.GetLocation(this.ReservedTable!.CurrentLocation)!, tiles, NpcExtensions.SitDownBehavior);
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
            NPC member = this.Members[i];
            if (!member.PathTo(location, tilePositions[i], 3, endBehavior))
                return;

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
    }
}
