using StardewMods.FoodJoints.Framework.Enums;
using StardewMods.FoodJoints.Framework.Game;
using StardewMods.FoodJoints.Framework.Objects;
using StardewValley.Pathfinding;
using xTile.Dimensions;

namespace StardewMods.FoodJoints.Framework.Characters;

public static class NpcExtensions
{
    internal static Point? FindNearestPointToChair(NPC npc, GameLocation startLocation, GameLocation targetLocation, Point seatPos)
    {
        List<sbyte[]> directions =
        [
            [0, 1], // down
            [-1, 0], // left
            [0, -1], // up
            [1, 0] // right
        ];

        Point entryPoint;
        string[]? locationPath = WarpPathfindingCache.GetLocationRoute(startLocation.NameOrUniqueName, targetLocation.NameOrUniqueName, Gender.Undefined)
            ?? Mod.Pathfinding.GetLocationRoute(startLocation.NameOrUniqueName, targetLocation.NameOrUniqueName);

        if (locationPath == null)
            return null;

        GameLocation last = Game1.getLocationFromName(locationPath[locationPath.Length - 1]);
        if (locationPath.Length > 1)
        {
            GameLocation secondLast = Game1.getLocationFromName(locationPath[locationPath.Length - 2]);
            Point warpPointToLast = secondLast.getWarpPointTo(locationPath[locationPath.Length - 1]);
            entryPoint = secondLast.getWarpPointTarget(warpPointToLast);
            if (entryPoint == Point.Zero)
            {
                Warp w = secondLast.getWarpFromDoor(warpPointToLast);
                entryPoint = new Point(w.TargetX, w.TargetY);
            }
        }
        else
        {
            Warp w = last.GetFirstPlayerWarp();
            entryPoint = new Point(w.X, w.Y);
        }

        Stack<Point>? shortestPath = null;
        foreach (sbyte[]? direction in directions)
        {
            Point newTile = seatPos + new Point(direction[0], direction[1]);

            if (targetLocation.GetFurnitureAt(newTile.ToVector2()) != null
                || !targetLocation.isTilePassable(new Location(newTile.X, newTile.Y), Game1.viewport))
                continue;

            Stack<Point>? p = Mod.Pathfinding.FindPath(entryPoint, newTile, targetLocation, npc);

            if (p != null && !(p.Count >= shortestPath?.Count))
                shortestPath = p;
        }

        return shortestPath?.LastOrDefault();
    }

    public static void SitDownEndBehavior(Character ch, GameLocation loc)
    {
        NPC c = (ch as NPC)!;

        Seat? seat = c.get_Seat();
        CustomerGroup? group = c.get_Group();

        if (seat != null && group is { ReservedTable: not null })
        {
            c.faceDirection(seat.SittingDirection);
            //c.JumpTo(seat.SittingPosition);
            c.get_IsSittingDown().Set(true);

            if (c.Name.StartsWith(Values.CUSTOMER_NPC_NAME_PREFIX))
            {
                // Is a custom customer model or randomly generated sprite.
                // Make them do the sitting frame
                int frame = seat.SittingDirection switch
                {
                    0 => 19,
                    1 => 17,
                    2 => 16,
                    3 => 18,
                    _ => -1
                };

                if (frame != -1)
                    c.Sprite.setCurrentAnimation([new FarmerSprite.AnimationFrame(frame, int.MaxValue)]);
            }
            else
            {
                // Is a villager NPC
                Mod.Dialogue.AddDialoguesOnArrivingAtCafe(c);
            }
            
            if (!group.Members.Any(other => !other.get_IsSittingDown().Value))
                group.ReservedTable.State.Set(TableState.CustomersThinkingOfOrder);
        }
    }

    public static void WarpThroughLocationsUntilNoFarmers(this NPC me)
    {
        GameLocation location = me.currentLocation;

        while (!me.currentLocation.Equals(Mod.Locations.Signboard?.Location) &&
               !me.currentLocation.Name.Equals("Farm") &&
               me.controller.pathToEndPoint is { Count: > 2 })
        {
            if (!location.Equals(me.currentLocation))
                return;
            
            me.controller.pathToEndPoint.Pop();
            me.controller.handleWarps(new Microsoft.Xna.Framework.Rectangle(me.controller.pathToEndPoint.Peek().X * 64, me.controller.pathToEndPoint.Peek().Y * 64, 64, 64));
            me.Position = new Vector2(me.controller.pathToEndPoint.Peek().X * 64, me.controller.pathToEndPoint.Peek().Y * 64 + 16);
        }
    }

}
