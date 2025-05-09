using StardewMods.FoodJoints.Framework.Enums;
using StardewMods.FoodJoints.Framework.Game;
using StardewMods.FoodJoints.Framework.Objects;
using StardewMods.SheepCore.Framework.Services;
using StardewValley.Pathfinding;

namespace StardewMods.FoodJoints.Framework.Characters;

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

    internal bool GoToTable()
    {
        List<Seat> seats = this.GetSeats();
        List<Point> seatPositions = seats.Select(seat => seat.TilePosition).ToList();

        GameLocation targetLocation = Game1.getLocationFromName(this.ReservedTable!.Location);

        for (int i = 0; i < this.Members.Count; i++)
        {
            NPC npc = this.Members[i];
            npc.EventActor = true; // For NPCBarrier

            Point seatPos = seatPositions[i];
            Point? targetPos = NpcExtensions.FindNearestPointToChair(npc, npc.currentLocation, targetLocation, seatPos);

            if (!targetPos.HasValue)
            {
                return false;
            }

            int jumpdirection;
            int x = targetPos.Value.X - seatPos.X;
            int y = seatPos.Y - targetPos.Value.Y;
            if (x == 1)
                jumpdirection = 1; // right
            else if (x == -1)
                jumpdirection = 3; // left
            else if (y == 1)
                jumpdirection = 2; // down
            else
                jumpdirection = 0; // up

            int facingDirection = seats[i].SittingDirection;
            string sitBehaviorName = $"sit_{jumpdirection}_{facingDirection}";

            if (npc.MoveTo(targetLocation, targetPos.Value, (n) =>
            {
                n.StartActivityRouteEndBehavior(sitBehaviorName, null);
                this.ReservedTable.State.Set(TableState.CustomersThinkingOfOrder);
            }))
            {
                return false;
            }

            AccessTools.Method(typeof(NPC), "prepareToDisembarkOnNewSchedulePath", [])?.Invoke(npc, []);
        }

        return true;
    }

    internal bool MoveTo(GameLocation location, Point tile, Action<NPC> endBehavior)
    {
        List<Point> tiles = this.Members.Select(_ => tile).ToList();
        return this.MoveTo(location, tiles, endBehavior);
    }

    internal bool MoveTo(GameLocation location, Point tile, string endBehaviorName)
    {
        List<Point> tiles = this.Members.Select(_ => tile).ToList();
        return this.MoveTo(location, tiles, endBehaviorName);
    }

    internal bool MoveTo(GameLocation location, List<Point> tilePositions, Action<NPC> endBehavior)
    {
        for (int i = 0; i < this.Members.Count; i++)
        {
            NPC npc = this.Members[i];
            npc.MoveTo(location, tilePositions[i], endBehavior);
            //this.Members[i].PathTo(location, tilePositions[i], 3, endBehavior);
        }

        return true;
    }

    internal bool MoveTo(GameLocation location, List<Point> tilePositions, string endBehaviorName)
    {
        for (int i = 0; i < this.Members.Count; i++)
        {
            NPC npc = this.Members[i];
            if (!npc.MoveTo(location, tilePositions[i], (n) => n.StartActivityRouteEndBehavior(endBehaviorName, null)))
                return false;
        }

        return true;
    }
}
