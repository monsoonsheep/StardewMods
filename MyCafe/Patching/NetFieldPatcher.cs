using System.Diagnostics.CodeAnalysis;
using HarmonyLib;
using MonsoonSheep.Stardew.Common.Patching;
using MyCafe.Game;
using MyCafe.Netcode;
using Netcode;
using StardewModdingAPI;
using StardewValley;

namespace MyCafe.Patching;

internal class NetFieldPatcher : BasePatcher
{
    public override void Apply(Harmony harmony, IMonitor monitor)
    {
        harmony.Patch(
            original: AccessTools.Constructor(typeof(FarmerTeam)),
            postfix: this.GetHarmonyMethod(nameof(NetFieldPatcher.After_FarmerTeamConstructor))
        );
        harmony.Patch(
            original: this.RequireMethod<Character>("initNetFields"),
            postfix: this.GetHarmonyMethod(nameof(NetFieldPatcher.After_NpcInitNetFields))
        );
    }

    /// <summary>
    /// Add the Cafe net field to the farm, and update <see cref="Mod.Cafe"/>
    /// </summary>
    private static void After_FarmerTeamConstructor(FarmerTeam __instance)
    {
        NetRef<CafeNetObject> val = __instance.get_CafeNetFields();
        __instance.NetFields.AddField(val);
        Log.Info("Adding netfields to FarmerTeam");
    }

    /// <summary>
    /// Add net fields to NPCs
    /// </summary>
    private static void After_NpcInitNetFields(NPC __instance)
    {
        __instance.NetFields
            .AddField(__instance.get_OrderItem(), $"{Mod.UniqueId}.Character.OrderItem")
            .AddField(__instance.get_DrawName(), $"{Mod.UniqueId}.Character.DrawName")
            .AddField(__instance.get_DrawOrderItem(), $"{Mod.UniqueId}.Character.DrawOrderItem")
            .AddField(__instance.get_IsSittingDown(), $"{Mod.UniqueId}.Character.IsSittingDown");
    }
}
