using System.Diagnostics.CodeAnalysis;
using System.Linq;
using HarmonyLib;
using MonsoonSheep.Stardew.Common.Patching;
using MyCafe.Characters;
using MyCafe.Locations.Objects;
using MyCafe.Netcode;
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
            original: this.RequireMethod<GameLocation>(nameof(GameLocation.checkAction)),
            prefix: this.GetHarmonyMethod(nameof(ActionPatcher.Before_GameLocationCheckAction))
        );
        harmony.Patch(
            original: this.RequireMethod<NPC>(nameof(NPC.checkAction)),
            prefix: this.GetHarmonyMethod(nameof(ActionPatcher.Before_NpcCheckAction))
        );
    }

    private static bool Before_NpcCheckAction(NPC __instance, Farmer who, GameLocation l, ref bool __result)
    {
        if (((Mod.Cafe.NpcCustomers.Contains(__instance.Name) || __instance.Name.StartsWith(ModKeys.CUSTOMER_NPC_NAME_PREFIX)) && !__instance.IsInvisible) && (l.Equals(Mod.Cafe.Indoor) || l.Equals(Mod.Cafe.Outdoor)))
        {
            CustomerGroup? group = Mod.Cafe.Groups.FirstOrDefault(g => g.Members.Contains(__instance));
            Table? table = group?.ReservedTable;
            if (table == null)
                return true;
            
            if (Mod.Cafe.InteractWithTable(table, who))
            {
                __result = true;
                return false;
            }
        }

        return true;
    }

    private static bool Before_GameLocationCheckAction(GameLocation __instance, Location tileLocation, Rectangle _, Farmer who, ref bool __result)
    {
        if ((__instance.Equals(Mod.Cafe.Indoor) || __instance.Equals(Mod.Cafe.Outdoor)))
        {
            foreach (Table table in Mod.Cafe.Tables)
            {
                if (table.BoundingBox.Value.Contains(tileLocation.X * 64, tileLocation.Y * 64)
                    && table.CurrentLocation == __instance.Name
                    && Mod.Cafe.InteractWithTable(table, who))
                {
                    __result = true;
                    return false;
                }
            }
        }

        return true;
    }
}
