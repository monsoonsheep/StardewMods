using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using MonsoonSheep.Stardew.Common.Patching;
using MyCafe.Locations.Objects;
using StardewModdingAPI;
using StardewValley;
using xTile.Dimensions;
using Rectangle = Microsoft.Xna.Framework.Rectangle;

namespace MyCafe.Patching;

[SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Harmony patching requirement")]
internal class ActionPatcher : BasePatcher
{
    public override void Apply(Harmony harmony, IMonitor monitor)
    {
        harmony.Patch(
            original: this.RequireMethod<GameLocation>("checkAction"),
            postfix: this.GetHarmonyMethod(nameof(ActionPatcher.After_CheckAction))
        );
    }

    private static void After_CheckAction(GameLocation __instance, Location tileLocation, Rectangle viewport, Farmer who, ref bool __result)
    {
        if (__result == true || (!__instance.Equals(Mod.Cafe.Indoor) && !__instance.Equals(Mod.Cafe.Outdoor)))
            return;

        foreach (Table table in Mod.Cafe.Tables)
        {
            if (table.BoundingBox.Value.Contains(tileLocation.X * 64, tileLocation.Y * 64)
                && table.CurrentLocation == __instance.Name
                && Mod.Cafe.InteractWithTable(table, who))
            {
                __result = true;
                return;
            }
        }
    }
}
