using MyCafe.Locations;
using StardewModdingAPI;
using StardewValley;
using System.Collections.Generic;
using System.Linq;
using StardewValley.Network;
using xTile.Dimensions;
using Rectangle = xTile.Dimensions.Rectangle;

namespace MyCafe.Patching;

internal class GameLocationPatches : PatchCollection
{
    public GameLocationPatches()
    {
        Patches =
        [
            new(
                typeof(GameLocation),
                "checkAction",
                [typeof(Location), typeof(Rectangle), typeof(Farmer)],
                postfix: CheckActionPostfix),

            new(
                typeof(Farm),
                "initNetFields",
                null,
                postfix: FarmInitNetFieldsPostfix),

        ];
    }

    private static void FarmInitNetFieldsPostfix(Farm __instance)
    {
        __instance.NetFields.AddField(__instance.get_Cafe(), "MonsoonSheep.MyCafe.Cafe");
    }

    private static void CheckActionPostfix(GameLocation __instance, Location tileLocation, Rectangle viewport, Farmer who, ref bool __result)
    {
        if (__result == true || (!__instance.Equals(Mod.Cafe.Indoor) && !__instance.Equals(Mod.Cafe.Outdoor)))
            return;

        foreach (Table table in Mod.Cafe.Tables)
        {
            if (table.BoundingBox.Value.Contains(tileLocation.X * 64, tileLocation.Y * 64) 
                && Mod.Cafe.InteractWithTable(table, who))
            {
                __result = true;
                return;
            }
        }
    }
}