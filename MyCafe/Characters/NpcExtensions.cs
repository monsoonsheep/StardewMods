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
using Netcode;
using StardewValley;
using StardewValley.Pathfinding;
using SUtility = StardewValley.Utility;

#pragma warning disable IDE1006

namespace MyCafe.Characters;

internal class CustomerState
{
    public readonly NetRef<Item> OrderItem = [];
    public readonly NetBool DrawName = [];
    public readonly NetBool DrawOrderItem = [];

    public CustomerGroup? Group;
    public Seat? Seat;

    public bool IsSittingDown;
    public Action<Character>? AfterLerp;

    public Vector2 LerpStartPosition;
    public Vector2 LerpEndPosition;
    public float LerpPosition = -1f;
    public float LerpDuration = -1f; 
}


public static class NpcExtensions
{
    internal static ConditionalWeakTable<Character, CustomerState> Values = [];

    internal static void RegisterProperties(ISpaceCoreApi spaceCore)
    {
        //TODO: Do we need to register these for serialization? 
        //spaceCore.RegisterCustomProperty(
        //    typeof(NpcExtensions),
        //    "OrderItem",
        //    typeof(NetRef<Item>),
        //    AccessTools.Method(typeof(NpcExtensions), nameof(get_OrderItem)),
        //    AccessTools.Method(typeof(NpcExtensions), nameof(set_OrderItem)));
        //spaceCore.RegisterCustomProperty(
        //    typeof(NpcExtensions),
        //    "DrawName",
        //    typeof(NetBool),
        //    AccessTools.Method(typeof(NpcExtensions), nameof(get_DrawName)),
        //    AccessTools.Method(typeof(NpcExtensions), nameof(set_DrawName)));
        //spaceCore.RegisterCustomProperty(
        //    typeof(NpcExtensions),
        //    "DrawOrderItem",
        //    typeof(NetBool),
        //    AccessTools.Method(typeof(NpcExtensions), nameof(get_DrawOrderItem)),
        //    AccessTools.Method(typeof(NpcExtensions), nameof(set_DrawOrderItem)));
    }

    public static NetRef<Item> get_OrderItem(this Character character)
    {
        CustomerState holder = Values.GetOrCreateValue(character);
        return holder.OrderItem;
    }

    public static void set_OrderItem(this Character character, NetRef<Item> value) { }

    public static NetBool get_DrawName(this Character character)
    {
        CustomerState holder = Values.GetOrCreateValue(character);
        return holder.DrawName;
    }

    public static void set_DrawName(this Character character, NetBool value) { }

    public static NetBool get_DrawOrderItem(this Character character)
    {
        CustomerState holder = Values.GetOrCreateValue(character);
        return holder.DrawOrderItem;
    }

    public static void set_DrawOrderItem(this Character character, NetBool value) { }

    public static CustomerGroup? get_Group(this Character character)
    {
        CustomerState holder = Values.GetOrCreateValue(character);
        return holder.Group;
    }

    public static void set_Group(this Character character, CustomerGroup? value)
    {
        CustomerState holder = Values.GetOrCreateValue(character);
        holder.Group = value;
    }

    public static Seat? get_Seat(this Character character)
    {
        CustomerState holder = Values.GetOrCreateValue(character);
        return holder.Seat;
    }

    public static void set_Seat(this Character character, Seat? value)
    {
        CustomerState holder = Values.GetOrCreateValue(character);
        holder.Seat = value;
    }

    public static bool get_IsSittingDown(this Character character)
    {
        CustomerState holder = Values.GetOrCreateValue(character);
        return holder.IsSittingDown;
    }

    public static void set_IsSittingDown(this Character character, bool value)
    {
        CustomerState holder = Values.GetOrCreateValue(character);
        holder.IsSittingDown = value;
    }

    public static Action<Character>? get_AfterLerp(this Character character)
    {
        CustomerState holder = Values.GetOrCreateValue(character);
        return holder.AfterLerp;
    }

    public static void set_AfterLerp(this Character character, Action<Character>? value)
    {
        CustomerState holder = Values.GetOrCreateValue(character);
        holder.AfterLerp = value;
    }

    public static Vector2 get_LerpStartPosition(this Character character)
    {
        CustomerState holder = Values.GetOrCreateValue(character);
        return holder.LerpStartPosition;
    }

    public static void set_LerpStartPosition(this Character character, Vector2 value)
    {
        CustomerState holder = Values.GetOrCreateValue(character);
        holder.LerpStartPosition = value;
    }

    public static Vector2 get_LerpEndPosition(this Character character)
    {
        CustomerState holder = Values.GetOrCreateValue(character);
        return holder.LerpEndPosition;
    }

    public static void set_LerpEndPosition(this Character character, Vector2 value)
    {
        CustomerState holder = Values.GetOrCreateValue(character);
        holder.LerpEndPosition = value;
    }

    public static float get_LerpPosition(this Character character)
    {
        CustomerState holder = Values.GetOrCreateValue(character);
        return holder.LerpPosition;
    }

    public static void set_LerpPosition(this Character character, float value)
    {
        CustomerState holder = Values.GetOrCreateValue(character);
        holder.LerpPosition = value;
    }

    public static float get_LerpDuration(this Character character)
    {
        CustomerState holder = Values.GetOrCreateValue(character);
        return holder.LerpDuration;
    }

    public static void set_LerpDuration(this Character character, float value)
    {
        CustomerState holder = Values.GetOrCreateValue(character);
        holder.LerpDuration = value;
    }

    public static void Freeze(this Character me)
    {
        AccessTools.Field(typeof(Character), "freezeMotion").SetValue(me, true);
    }

    public static void Unfreeze(this Character me)
    {
        AccessTools.Field(typeof(Character), "freezeMotion").SetValue(me, false);
    }

    public static void Jump(this Character me, int direction)
    {
        Vector2 sitPosition = me.Position + CommonHelper.DirectionIntToDirectionVector(direction) * 64f;
        me.set_LerpStartPosition(me.Position);
        me.set_LerpEndPosition(sitPosition);
        me.set_LerpPosition(0f);
        me.set_LerpDuration(0.2f);
    }

    public static PathFindController.endBehavior SitDownBehavior = delegate (Character c, GameLocation _)
    {
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
        Mod.NpcCustomers.Remove(npc.Name);
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
        GameLocation targetLocation = Game1.getLocationFromName(originalPathDescription.targetLocationName);
        Stack<Point>? routeToScheduleItem = Pathfinding.PathfindFromLocationToLocation(
            npc.currentLocation,
            npc.TilePoint,
            targetLocation,
            originalPathDescription.targetTile,
            npc);

        Log.Trace($"Schedule description is {targetLocation.Name}: {originalPathDescription.targetTile}, behavior: {originalPathDescription.endOfRouteBehavior}");
        if (routeToScheduleItem == null)
        {
            Log.Trace("Can't find route back");
            // TODO: Warp them to their home
            return;
        }

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
