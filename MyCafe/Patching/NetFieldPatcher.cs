using System.Diagnostics.CodeAnalysis;
using HarmonyLib;
using MonsoonSheep.Stardew.Common.Patching;
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
            postfix: this.GetHarmonyMethod(nameof(NetFieldPatcher.After_InitNetFields))
            );
    }

    /// <summary>
    /// Add the Cafe net field to the Farm location, and update <see cref="Mod.Cafe"/>
    /// </summary>
    private static void After_InitNetFields(Farm __instance)
    {
        // Should it be "Cafe"?
        __instance.NetFields.AddField(__instance.get_Cafe(), $"{Mod.UniqueId}.Cafe");
        Mod.Instance.CafeField = __instance.get_Cafe();
    }
}
