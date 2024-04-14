using System;
using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;
using MyCafe.Characters;
using MyCafe.Interfaces;
using MyCafe.Locations.Objects;
using Netcode;
using StardewValley;

namespace MyCafe.Game;
internal static class NpcVirtualProperties
{
    internal class Holder
    {
        public readonly NetRef<Item> OrderItem = [];
        public readonly NetBool DrawName = [false];
        public readonly NetBool DrawOrderItem = [false];
        public NetBool IsSittingDown = [false];

        public CustomerGroup? Group;
        public Seat? Seat;

        public Action<NPC>? AfterLerp;

        public Vector2 LerpStartPosition;
        public Vector2 LerpEndPosition;
        public float LerpPosition = -1f;
        public float LerpDuration = -1f;
    }

    internal static ConditionalWeakTable<NPC, Holder> Values = [];

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

    public static NetRef<Item> get_OrderItem(this NPC npc)
    {
        Holder holder = Values.GetOrCreateValue(npc);
        return holder.OrderItem;
    }

    public static void set_OrderItem(this NPC npc, NetRef<Item> value) { }

    public static NetBool get_DrawName(this NPC npc)
    {
        Holder holder = Values.GetOrCreateValue(npc);
        return holder.DrawName;
    }

    public static void set_DrawName(this NPC npc, NetBool value) { }

    public static NetBool get_DrawOrderItem(this NPC npc)
    {
        Holder holder = Values.GetOrCreateValue(npc);
        return holder.DrawOrderItem;
    }

    public static void set_DrawOrderItem(this NPC npc, NetBool value) { }

    public static CustomerGroup? get_Group(this NPC npc)
    {
        Holder holder = Values.GetOrCreateValue(npc);
        return holder.Group;
    }

    public static void set_Group(this NPC npc, CustomerGroup? value)
    {
        Holder holder = Values.GetOrCreateValue(npc);
        holder.Group = value;
    }

    public static Seat? get_Seat(this NPC npc)
    {
        Holder holder = Values.GetOrCreateValue(npc);
        return holder.Seat;
    }

    public static void set_Seat(this NPC npc, Seat? value)
    {
        Holder holder = Values.GetOrCreateValue(npc);
        holder.Seat = value;
    }

    public static NetBool get_IsSittingDown(this NPC npc)
    {
        Holder holder = Values.GetOrCreateValue(npc);
        return holder.IsSittingDown;
    }

    public static void set_IsSittingDown(this NPC npc, NetBool value)
    {
        Holder holder = Values.GetOrCreateValue(npc);
        holder.IsSittingDown = value;
    }

    public static Action<NPC>? get_AfterLerp(this NPC npc)
    {
        Holder holder = Values.GetOrCreateValue(npc);
        return holder.AfterLerp;
    }

    public static void set_AfterLerp(this NPC npc, Action<NPC>? value)
    {
        Holder holder = Values.GetOrCreateValue(npc);
        holder.AfterLerp = value;
    }

    public static Vector2 get_LerpStartPosition(this NPC npc)
    {
        Holder holder = Values.GetOrCreateValue(npc);
        return holder.LerpStartPosition;
    }

    public static void set_LerpStartPosition(this NPC npc, Vector2 value)
    {
        Holder holder = Values.GetOrCreateValue(npc);
        holder.LerpStartPosition = value;
    }

    public static Vector2 get_LerpEndPosition(this NPC npc)
    {
        Holder holder = Values.GetOrCreateValue(npc);
        return holder.LerpEndPosition;
    }

    public static void set_LerpEndPosition(this NPC npc, Vector2 value)
    {
        Holder holder = Values.GetOrCreateValue(npc);
        holder.LerpEndPosition = value;
    }

    public static float get_LerpPosition(this NPC npc)
    {
        Holder holder = Values.GetOrCreateValue(npc);
        return holder.LerpPosition;
    }

    public static void set_LerpPosition(this NPC npc, float value)
    {
        Holder holder = Values.GetOrCreateValue(npc);
        holder.LerpPosition = value;
    }

    public static float get_LerpDuration(this NPC npc)
    {
        Holder holder = Values.GetOrCreateValue(npc);
        return holder.LerpDuration;
    }

    public static void set_LerpDuration(this NPC npc, float value)
    {
        Holder holder = Values.GetOrCreateValue(npc);
        holder.LerpDuration = value;
    }

}
