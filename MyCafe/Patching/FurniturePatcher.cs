using System.Diagnostics.CodeAnalysis;
using System.Linq;
using HarmonyLib;
using Microsoft.Xna.Framework;
using MonsoonSheep.Stardew.Common.Patching;
using MyCafe.Locations.Objects;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Objects;

namespace MyCafe.Patching;

[SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Harmony patching requirement")]
internal class FurniturePatcher : BasePatcher
{
    public override void Apply(Harmony harmony, IMonitor monitor)
    {
        harmony.Patch(
            original: this.RequireMethod<Furniture>(nameof(Furniture.GetAdditionalFurniturePlacementStatus)),
            postfix: this.GetHarmonyMethod(nameof(After_GetAdditionalFurniturePlacementStatus))
        );
        harmony.Patch(
            original: this.RequireMethod<Furniture>(nameof(Furniture.performObjectDropInAction)),
            prefix: this.GetHarmonyMethod(nameof(Before_PerformObjectDropInAction))
        );
        harmony.Patch(
            original: this.RequireMethod<Furniture>(nameof(Furniture.canBeRemoved)),
            postfix: this.GetHarmonyMethod(nameof(After_CanBeRemoved))
        );
        harmony.Patch(
            original: this.RequireMethod<Furniture>(nameof(Furniture.AddSittingFarmer)),
            prefix: this.GetHarmonyMethod(nameof(Before_AddSittingFarmer))
        );
    }

    /// <summary>
    /// To avoid putting furniture on top of tables that are registered (probably? I forgor)
    /// </summary>
    private static void After_GetAdditionalFurniturePlacementStatus(Furniture __instance, GameLocation location, int x, int y, Farmer who, ref int __result)
    {
        if (Utility.IsTable(__instance))
        {
            Furniture table = location.GetFurnitureAt(new Vector2(x, y));
            if (Mod.Cafe.TryGetFurnitureTable(table, out FurnitureTable trackedTable) && trackedTable.IsReserved)
                __result = 2;
        }
    }

    /// <summary>
    /// Prevent farmers from sitting in chairs that are reserved for customers
    /// </summary>
    private static bool Before_AddSittingFarmer(Furniture __instance, Farmer who, ref Vector2? __result)
    {
        if (Utility.IsChair(__instance)
            && Mod.Cafe.Tables
                .Any(t => t.Seats
                    .OfType<FurnitureSeat>()
                    .Any(s => s.IsReserved && s.ActualChair.Value.Equals(__instance))))
        {
            Log.Warn("Can't sit in this chair, it's reserved");
            __result = null;
            return false;
        }

        return true;
    }

    /// <summary>
    /// 
    /// </summary>
    private static bool Before_PerformObjectDropInAction(Furniture __instance, Item dropInItem, bool probe, Farmer who, ref bool __result)
    {
        if (Utility.IsTable(__instance))
        {
            if (Mod.Cafe.TryGetFurnitureTable(__instance, out FurnitureTable trackedTable)
                && trackedTable.IsReserved)
            {
                Log.Warn("Can't drop in this object onto this table. It's reserved");
                __result = false;
                return false;
            }
        }

        return true;
    }

    private static void After_CanBeRemoved(Furniture __instance, Farmer who, ref bool __result)
    {
        if (__result is false)
            return;

        if (Utility.IsTable(__instance))
        {
            if (Mod.Cafe.TryGetFurnitureTable(__instance, out FurnitureTable trackedTable) && trackedTable.IsReserved)
            {
                Game1.addHUDMessage(new HUDMessage("Can't remove this furniture", 1000, fadeIn: false));
                __result = false;
            }
        }
    }
}
