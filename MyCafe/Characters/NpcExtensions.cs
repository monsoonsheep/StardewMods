using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
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

#pragma warning disable IDE1006

namespace MyCafe.Characters;
public static class NpcExtensions
{
    internal class CustomerData
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

    internal static ConditionalWeakTable<Character, CustomerData> Values = [];

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
        CustomerData holder = Values.GetOrCreateValue(character);
        return holder.OrderItem;
    }

    public static void set_OrderItem(this Character character, NetRef<Item> value) { }

    public static NetBool get_DrawName(this Character character)
    {
        CustomerData holder = Values.GetOrCreateValue(character);
        return holder.DrawName;
    }

    public static void set_DrawName(this Character character, NetBool value) { }

    public static NetBool get_DrawOrderItem(this Character character)
    {
        CustomerData holder = Values.GetOrCreateValue(character);
        return holder.DrawOrderItem;
    }

    public static void set_DrawOrderItem(this Character character, NetBool value) { }

    public static CustomerGroup? get_Group(this Character character)
    {
        CustomerData holder = Values.GetOrCreateValue(character);
        return holder.Group;
    }

    public static void set_Group(this Character character, CustomerGroup? value)
    {
        CustomerData holder = Values.GetOrCreateValue(character);
        holder.Group = value;
    }

    public static Seat? get_Seat(this Character character)
    {
        CustomerData holder = Values.GetOrCreateValue(character);
        return holder.Seat;
    }

    public static void set_Seat(this Character character, Seat? value)
    {
        CustomerData holder = Values.GetOrCreateValue(character);
        holder.Seat = value;
    }

    public static bool get_IsSittingDown(this Character character)
    {
        CustomerData holder = Values.GetOrCreateValue(character);
        return holder.IsSittingDown;
    }

    public static void set_IsSittingDown(this Character character, bool value)
    {
        CustomerData holder = Values.GetOrCreateValue(character);
        holder.IsSittingDown = value;
    }

    public static Action<Character>? get_AfterLerp(this Character character)
    {
        CustomerData holder = Values.GetOrCreateValue(character);
        return holder.AfterLerp;
    }

    public static void set_AfterLerp(this Character character, Action<Character>? value)
    {
        CustomerData holder = Values.GetOrCreateValue(character);
        holder.AfterLerp = value;
    }

    public static Vector2 get_LerpStartPosition(this Character character)
    {
        CustomerData holder = Values.GetOrCreateValue(character);
        return holder.LerpStartPosition;
    }

    public static void set_LerpStartPosition(this Character character, Vector2 value)
    {
        CustomerData holder = Values.GetOrCreateValue(character);
        holder.LerpStartPosition = value;
    }

    public static Vector2 get_LerpEndPosition(this Character character)
    {
        CustomerData holder = Values.GetOrCreateValue(character);
        return holder.LerpEndPosition;
    }

    public static void set_LerpEndPosition(this Character character, Vector2 value)
    {
        CustomerData holder = Values.GetOrCreateValue(character);
        holder.LerpEndPosition = value;
    }

    public static float get_LerpPosition(this Character character)
    {
        CustomerData holder = Values.GetOrCreateValue(character);
        return holder.LerpPosition;
    }

    public static void set_LerpPosition(this Character character, float value)
    {
        CustomerData holder = Values.GetOrCreateValue(character);
        holder.LerpPosition = value;
    }

    public static float get_LerpDuration(this Character character)
    {
        CustomerData holder = Values.GetOrCreateValue(character);
        return holder.LerpDuration;
    }

    public static void set_LerpDuration(this Character character, float value)
    {
        CustomerData holder = Values.GetOrCreateValue(character);
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
}
