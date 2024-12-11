using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StardewValley.Pathfinding;
using StardewValley;

namespace StardewMods.SheepCore.Framework.Services;
public static class NpcExtensions
{
    public static bool MoveTo(this NPC me, GameLocation startLocation, Point startPoint, GameLocation targetLocation, Point targetPoint, PathFindController.endBehavior? endBehavior)
    {
        Stack<Point>? path = Mod.Pathfinding.PathfindFromLocationToLocation(startLocation, startPoint, targetLocation, targetPoint, me);
        if (path == null)
            return false;
        me.controller = new PathFindController(path, me.currentLocation, me, path.Last())
        {
            NPCSchedule = true
        };

        me.controller.endBehaviorFunction = endBehavior;

        return true;
    }

    public static bool MoveTo(this NPC me, GameLocation location, Point tilePoint, PathFindController.endBehavior? endBehavior)
    {
        return me.MoveTo(me.currentLocation, me.TilePoint, location, tilePoint, endBehavior);
    }
}
