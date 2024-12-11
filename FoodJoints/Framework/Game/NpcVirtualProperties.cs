using System;
using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;
using Netcode;
using StardewMods.FoodJoints.Framework.Characters;
using StardewMods.FoodJoints.Framework.Objects;
using StardewValley;

namespace StardewMods.FoodJoints.Framework.Game;
internal static class NpcVirtualProperties
{
    internal class Holder
    {
        public readonly NetRef<Item?> OrderItem = [];
        public readonly NetBool DrawName = [false];
        public readonly NetBool DrawOrderItem = [false];
        public NetBool IsSittingDown = [false];

        public CustomerGroup? Group;
        public Seat? Seat;
    }

    internal static ConditionalWeakTable<NPC, Holder> Table = [];

    internal static void InjectFields()
    {
        Mod.Harmony.Patch(
           original: AccessTools.Constructor(typeof(NPC), []),
           postfix: new HarmonyMethod(AccessTools.Method(typeof(NpcVirtualProperties), nameof(After_NPCConstructor)))
       );
    }

    /// <summary>
    /// Add net fields to NPC
    /// </summary>
    private static void After_NPCConstructor(NPC __instance)
    {
        __instance.NetFields
            .AddField(__instance.get_DrawName(), "DrawName")
            .AddField(__instance.get_DrawOrderItem(), "DrawOrderItem")
            .AddField(__instance.get_IsSittingDown(), "IsSittingDown")
            .AddField(__instance.get_OrderItem(), "OrderItem");


        Log.Trace("Adding netfields to NPC");
    }

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

    public static NetRef<Item?> get_OrderItem(this NPC npc)
    {
        Holder holder = Table.GetOrCreateValue(npc);
        return holder.OrderItem;
    }

    public static void set_OrderItem(this NPC npc, NetRef<Item?> value) { }

    public static NetBool get_DrawName(this NPC npc)
    {
        Holder holder = Table.GetOrCreateValue(npc);
        return holder.DrawName;
    }

    public static void set_DrawName(this NPC npc, NetBool value) { }

    public static NetBool get_DrawOrderItem(this NPC npc)
    {
        Holder holder = Table.GetOrCreateValue(npc);
        return holder.DrawOrderItem;
    }

    public static void set_DrawOrderItem(this NPC npc, NetBool value) { }

    public static CustomerGroup? get_Group(this NPC npc)
    {
        Holder holder = Table.GetOrCreateValue(npc);
        return holder.Group;
    }

    public static void set_Group(this NPC npc, CustomerGroup? value)
    {
        Holder holder = Table.GetOrCreateValue(npc);
        holder.Group = value;
    }

    public static Seat? get_Seat(this NPC npc)
    {
        Holder holder = Table.GetOrCreateValue(npc);
        return holder.Seat;
    }

    public static void set_Seat(this NPC npc, Seat? value)
    {
        Holder holder = Table.GetOrCreateValue(npc);
        holder.Seat = value;
    }

    public static NetBool get_IsSittingDown(this NPC npc)
    {
        Holder holder = Table.GetOrCreateValue(npc);
        return holder.IsSittingDown;
    }

    public static void set_IsSittingDown(this NPC npc, NetBool value)
    {
        Holder holder = Table.GetOrCreateValue(npc);
        holder.IsSittingDown = value;
    }
}
