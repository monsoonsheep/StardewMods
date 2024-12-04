using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewValley.Pathfinding;

namespace StardewMods.SheepCore.Framework.Services;
public class NpcMovement
{
    public static NpcMovement Instance = null!;

    public NpcMovement()
        => Instance = this;

    public bool NpcPathTo(NPC npc, GameLocation location, Point tilePosition)
    {
        return this.NpcPathToFrom(npc, npc.currentLocation, npc.TilePoint, location, tilePosition);
    }

    public bool NpcPathToFrom(NPC npc, GameLocation startingLocation, Point startingTile, GameLocation targetLocation, Point targetTile)
    {
        SchedulePathDescription sched = npc.pathfindToNextScheduleLocation("",
        startingLocation.Name, startingTile.X, startingTile.Y,
        targetLocation.Name, targetTile.X, targetTile.Y,
        0, "", "");

        if (sched?.route == null || sched.route.Count == 0)
            return false;

        npc.controller = new PathFindController(sched.route, npc.currentLocation, npc, sched.route.Last())
        {
            NPCSchedule = true
        };

        return true;
    }
}
