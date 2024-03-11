using System.Diagnostics.CodeAnalysis;
using HarmonyLib;
using MonsoonSheep.Stardew.Common.Patching;
using MyCafe.Characters;
using MyCafe.Locations.Objects;
using StardewModdingAPI;
using StardewValley;
using xTile.Dimensions;

namespace MyCafe.Patching;

[SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Harmony patching requirement")]
internal class NetFieldPatcher : BasePatcher
{
    public override void Apply(Harmony harmony, IMonitor monitor)
    {
        harmony.Patch(
            original: this.RequireMethod<Farm>("initNetFields"),
            postfix: this.GetHarmonyMethod(nameof(NetFieldPatcher.After_FarmInitNetFields))
        );
        harmony.Patch(
            original: this.RequireMethod<Character>("initNetFields"),
            postfix: this.GetHarmonyMethod(nameof(NetFieldPatcher.After_CharacterInitNetFields))
        );
    }

    /// <summary>
    /// Add the Cafe net field to the Farm location, and update <see cref="Mod.Cafe"/>
    /// </summary>
    private static void After_FarmInitNetFields(Farm __instance)
    {
        // Should it be "Cafe"?
        __instance.NetFields.AddField(__instance.get_Cafe(), $"Cafe");
        Mod.Instance.CafeField = __instance.get_Cafe();
    }

    /// <summary>
    /// Add net fields to NPCs
    /// </summary>
    private static void After_CharacterInitNetFields(Character __instance)
    {
        __instance.NetFields
            .AddField(__instance.get_OrderItem(), $"{Mod.UniqueId}.Character.OrderItem")
            .AddField(__instance.get_DrawName(), $"{Mod.UniqueId}.Character.DrawName")
            .AddField(__instance.get_DrawOrderItem(), $"{Mod.UniqueId}.Character.DrawOrderItem");
    }
}
