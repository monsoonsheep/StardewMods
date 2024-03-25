using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using HarmonyLib;
using Microsoft.Xna.Framework;
using MonsoonSheep.Stardew.Common;
using MyCafe.Enums;
using MyCafe.Interfaces;
using MyCafe.Locations.Objects;
using MyCafe.Netcode;
using Netcode;
using StardewValley;
using StardewValley.Pathfinding;
using SUtility = StardewValley.Utility;

namespace MyCafe.Characters;

public static class NpcExtensions
{
    public static void Freeze(this NPC me)
    {
        AccessTools.Field(typeof(Character), "freezeMotion").SetValue(me, true);
    }

    public static void Unfreeze(this NPC me)
    {
        AccessTools.Field(typeof(Character), "freezeMotion").SetValue(me, false);
    }

    public static void Jump(this NPC me, int direction)
    {
        Vector2 sitPosition = me.Position + CommonHelper.DirectionIntToDirectionVector(direction) * 64f;
        me.set_LerpStartPosition(me.Position);
        me.set_LerpEndPosition(sitPosition);
        me.set_LerpPosition(0f);
        me.set_LerpDuration(0.2f);
    }

    public static PathFindController.endBehavior SitDownBehavior = delegate (Character ch, GameLocation _)
    {
        NPC c = (ch as NPC)!;

        Seat? seat = c.get_Seat();
        CustomerGroup? group = c.get_Group();

        if (seat != null && group is { ReservedTable: not null })
        {
            int direction = CommonHelper.DirectionIntFromVectors(c.Tile, seat.Position.ToVector2());
            c.faceDirection(seat.SittingDirection);

            c.Jump(direction);
            c.set_IsSittingDown(true);
            if (!group.Members.Any(other => !other.get_IsSittingDown()))
                group.ReservedTable.State.Set(TableState.CustomersThinkingOfOrder);
        }
    };

    internal static void ReturnToSchedule(this NPC npc)
    {
        Mod.Cafe.NpcCustomers.Remove(npc.Name);
        npc.eventActor = false;
        npc.ignoreScheduleToday = false;

        List<int> activityTimes = npc.Schedule.Keys.OrderBy(i => i).ToList();
        int timeOfCurrent = activityTimes.LastOrDefault(t => t <= Game1.timeOfDay);
        int timeOfNext = activityTimes.FirstOrDefault(t => t > Game1.timeOfDay);
        int minutesSinceCurrentStarted = SUtility.CalculateMinutesBetweenTimes(timeOfCurrent, Game1.timeOfDay);
        int minutesTillNextStarts = SUtility.CalculateMinutesBetweenTimes(Game1.timeOfDay, timeOfNext);
        int timeOfActivity;

        Log.Trace($"[{Game1.timeOfDay}] Returning {npc.Name} to schedule for key \"{npc.ScheduleKey}\". Time of current activity is {timeOfCurrent}, next activity is at {timeOfNext}.");

        if (timeOfCurrent == 0) // Means it's the start of the day
        {
            timeOfActivity = activityTimes.First();
        }
        else if (timeOfNext == 0) // Means it's the end of the day
        {
            timeOfActivity = activityTimes.Last();
        }
        else
        {
            if (minutesTillNextStarts < minutesSinceCurrentStarted && minutesTillNextStarts <= 30)
                // If we're very close to the next item, 
                timeOfActivity = timeOfNext;
            else
                timeOfActivity = timeOfCurrent;
        }

        Log.Trace($"Time of selected activity is {timeOfActivity}");

        SchedulePathDescription originalPathDescription = npc.Schedule[timeOfActivity];

        Log.Trace($"Schedule description is {originalPathDescription.targetLocationName}: {originalPathDescription.targetTile}, behavior: {originalPathDescription.endOfRouteBehavior}");

        GameLocation targetLocation = Game1.getLocationFromName(originalPathDescription.targetLocationName);
        Stack<Point>? routeToScheduleItem = Pathfinding.PathfindFromLocationToLocation(
            npc.currentLocation,
            npc.TilePoint,
            targetLocation,
            originalPathDescription.targetTile,
            npc);

        if (routeToScheduleItem == null)
        {
            Log.Trace("Can't find route back");
            // TODO: Warp them to their home
            return;
        }

        // Can this return null?
        SchedulePathDescription toInsert = npc.pathfindToNextScheduleLocation(
            npc.ScheduleKey,
            npc.currentLocation.Name,
            npc.TilePoint.X,
            npc.TilePoint.Y,
            originalPathDescription.targetLocationName,
            originalPathDescription.targetTile.X,
            originalPathDescription.targetTile.Y,
            originalPathDescription.facingDirection,
            originalPathDescription.endOfRouteBehavior,
            originalPathDescription.endOfRouteMessage);

        npc.queuedSchedulePaths.Clear();
        npc.Schedule[Game1.timeOfDay] = toInsert;
        npc.checkSchedule(Game1.timeOfDay);
    }
}
