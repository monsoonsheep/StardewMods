using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using Microsoft.CodeAnalysis.Text;
using Microsoft.Xna.Framework;
using StardewValley.Locations;
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

        sched.time = Game1.timeOfDay;

        npc.IsWalkingInSquare = false;
        AccessTools.Field(typeof(NPC), "nextSquarePosition").SetValue(npc, Vector2.Zero);
        AccessTools.Field(typeof(NPC), "returningToEndPoint").SetValue(npc, false);
        npc.Halt();

        npc.Schedule[Game1.timeOfDay] = sched;
        npc.checkSchedule(Game1.timeOfDay);

        if (npc.controller == null)
            return false;

        npc.controller.NPCSchedule = true;

        //npc.controller = new PathFindController(sched.route, npc.currentLocation, npc, sched.route.Last())
        //{
        //    NPCSchedule = true
        //};

        return true;
    }
}
