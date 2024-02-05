using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using Microsoft.Xna.Framework;
using MonsoonSheep.Stardew.Common.Patching;
using MyCafe.Locations.Objects;
using StardewModdingAPI;
using StardewValley;
using xTile.Dimensions;
using Rectangle = Microsoft.Xna.Framework.Rectangle;

namespace MyCafe.Patching;

[SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Harmony patching requirement")]
internal class LocationPatcher : BasePatcher
{
    public override void Apply(Harmony harmony, IMonitor monitor)
    {
        harmony.Patch(
            original: this.RequireMethod<GameLocation>("getWarpPointTo", [typeof(string), typeof(Character)]),
            postfix: this.GetHarmonyMethod(nameof(LocationPatcher.After_GetWarpPointTo))
        );
    }

    private static void After_GetWarpPointTo(GameLocation __instance, string location, Character? character, ref Point __result)
    {
        if (__result == Point.Zero && location == Mod.Cafe.Indoor?.Name)
        {
            Log.Trace("Replacing name with uniquename");
            __result = __instance.getWarpPointTo(Mod.Cafe.Indoor.uniqueName.Value, character);
        }
    }
}
