using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StardewValley.Pathfinding;
using StardewValley;
using System.Diagnostics.CodeAnalysis;

namespace StardewMods.SheepCore.Framework.Services;
public static class NpcExtensions
{
    public static bool CanPath(this NPC me, GameLocation location, Point position, [NotNullWhen(true)] out Stack<Point>? path, bool checkCharacter = true)
    {
        if (me == null)
        {
            path = null;
            return false;
        }

        path = Pathfinding.Instance.PathfindFromLocationToLocation(
            me.currentLocation,
            me.TilePoint,
            location,
            position,
            checkCharacter ? me : null);

        return path?.Any() == true;
    }

    public static void MoveTo(this NPC me, Stack<Point> path, Action<NPC>? endBehavior)
    {
        me.controller = new PathFindController(path, me.currentLocation, me, path.Last())
        {
            nonDestructivePathing = false,
            NPCSchedule = true
        };

        if (endBehavior != null)
            me.controller.endBehaviorFunction = Pathfinding.EndBehaviorDelegateFactory(me, endBehavior);
    }

    public static bool MoveTo(this NPC me, GameLocation startLocation, Point startPoint, GameLocation targetLocation, Point targetPoint, Action<NPC>? endBehavior)
    {
        Stack<Point>? path = Pathfinding.Instance.PathfindFromLocationToLocation(startLocation, startPoint, targetLocation, targetPoint, me);
        if (path == null)
            return false;

        me.MoveTo(path, endBehavior);

        return true;
    }

    public static bool MoveTo(this NPC me, GameLocation location, Point tilePoint, Action<NPC>? endBehavior)
    {
        if (me.CanPath(location, tilePoint, out Stack<Point>? path))
        {
            me.MoveTo(path, endBehavior);
            return true;
        }

        return false;
    }
}
