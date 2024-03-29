using System.Diagnostics.CodeAnalysis;
using HarmonyLib;
using MonsoonSheep.Stardew.Common.Patching;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.Menus;

namespace MyCafe.Patching;

[SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Harmony patching requirement")]
internal class SignboardPatcher : BasePatcher
{
    public override void Apply(Harmony harmony, IMonitor monitor)
    {

        harmony.Patch(
            original: this.RequireMethod<CarpenterMenu>("setUpForBuildingPlacement"),
            postfix: this.GetHarmonyMethod(nameof(After_SetUpForBuildingPlacement))
        );
        harmony.Patch(
            original: this.RequireMethod<Game1>("warpFarmer", [typeof(LocationRequest), typeof(int), typeof(int), typeof(int)]),
            postfix: this.GetHarmonyMethod(nameof(After_Game1WarpFarmer))
        );
    }

    private static void After_SetUpForBuildingPlacement(CarpenterMenu __instance)
    {
        Building? building = (Building?)AccessTools.Field(typeof(CarpenterMenu), "currentBuilding")?.GetValue(__instance);
        if (building != null && building.GetData()?.CustomFields.TryGetValue(ModKeys.CAFE_SIGNBOARD_CUSTOMFIELD, out string? value) is true && value == "true")
            Mod.Instance.IsPlacingSignBoard = true;
    }

    private static void After_Game1WarpFarmer(LocationRequest locationRequest, int tileX, int tileY, int facingDirectionAfterWarp)
    {
        Mod.Instance.IsPlacingSignBoard = false;
    }
}
