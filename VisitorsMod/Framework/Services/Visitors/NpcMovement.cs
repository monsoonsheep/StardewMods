using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewValley.Pathfinding;

namespace StardewMods.VisitorsMod.Framework.Services.Visitors;
internal class NpcMovement : Service
{
    public NpcMovement(
        Harmony harmony,
        ILogger logger,
        IManifest manifest
        ) : base(logger, manifest)
    {

    }

    internal bool NpcPathTo(NPC npc, GameLocation location, Point tilePosition)
    {
        return this.NpcPathToFrom(npc, npc.currentLocation, npc.TilePoint, location, tilePosition);
    }

    internal bool NpcPathToFrom(NPC npc, GameLocation startingLocation, Point startingTile, GameLocation targetLocation, Point targetTile)
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
