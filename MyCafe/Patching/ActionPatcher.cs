using System.Diagnostics.CodeAnalysis;
using System.Linq;
using HarmonyLib;
using Microsoft.Xna.Framework;
using MonsoonSheep.Stardew.Common.Patching;
using Monsoonsheep.StardewMods.MyCafe.Characters;
using Monsoonsheep.StardewMods.MyCafe.Enums;
using Monsoonsheep.StardewMods.MyCafe.Locations.Objects;
using Monsoonsheep.StardewMods.MyCafe.Netcode;
using Monsoonsheep.StardewMods.MyCafe.UI;
using StardewModdingAPI;
using StardewValley;
using xTile.Dimensions;
using Rectangle = Microsoft.Xna.Framework.Rectangle;
using SObject = StardewValley.Object;

namespace Monsoonsheep.StardewMods.MyCafe.Patching;

[SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Harmony patching requirement")]
internal class ActionPatcher : BasePatcher
{
    public override void Apply(Harmony harmony, IMonitor monitor)
    {
        harmony.Patch(
            original: this.RequireMethod<SObject>(nameof(SObject.performRemoveAction)),
            postfix: this.GetHarmonyMethod(nameof(ActionPatcher.After_ObjectPerformRemoveAction))
        );
        //harmony.Patch(
        //    original: this.RequireMethod<Farmer>(nameof(Farmer.OnItemReceived)),
        //    postfix: this.GetHarmonyMethod(nameof(ActionPatcher.After_FarmerOnItemReceived))
        //);
        harmony.Patch(
            original: this.RequireMethod<SObject>(nameof(SObject.placementAction)),
            postfix: this.GetHarmonyMethod(nameof(ActionPatcher.After_ObjectPlacementAction))
        );
        harmony.Patch(
            original: this.RequireMethod<SObject>(nameof(SObject.checkForAction), [typeof(Farmer), typeof(bool)]),
            postfix: this.GetHarmonyMethod(nameof(ActionPatcher.After_ObjectCheckForAction))
            );
        harmony.Patch(
            original: this.RequireMethod<GameLocation>(nameof(GameLocation.checkAction)),
            prefix: this.GetHarmonyMethod(nameof(ActionPatcher.Before_GameLocationCheckAction))
        );
        harmony.Patch(
            original: this.RequireMethod<NPC>(nameof(NPC.checkAction)),
            prefix: this.GetHarmonyMethod(nameof(ActionPatcher.Before_NpcCheckAction))
        );
    }

    private static void After_ObjectCheckForAction(SObject __instance, Farmer who, bool justCheckingForActivity, ref bool __result)
    {
        if (__instance.QualifiedItemId.Equals($"(BC){ModKeys.CAFE_SIGNBOARD_OBJECT_ID}") && !justCheckingForActivity && __result == false)
        {
            if (Game1.activeClickableMenu == null)
            {
                Game1.activeClickableMenu = new CafeMenu();
            }
        }
    }

    private static void After_ObjectPlacementAction(SObject __instance, GameLocation location, int x, int y, Farmer who, ref bool __result)
    {
        if (__instance is { QualifiedItemId: $"(BC){ModKeys.CAFE_SIGNBOARD_OBJECT_ID}" })
        {
            Log.Trace("Placed down signboard");
            Mod.Cafe.OnPlacedDownSignboard(__instance);
        }
    }

    private static void After_ObjectPerformRemoveAction(SObject __instance)
    {
        if (__instance is { QualifiedItemId: $"(BC){ModKeys.CAFE_SIGNBOARD_OBJECT_ID}" })
        {
            Log.Trace("Removed signboard");
            Mod.Cafe.OnRemovedSignboard(__instance);
        }
    }

    private static void After_FarmerOnItemReceived(Farmer __instance, Item? item, int countAdded, Item? mergedIntoStack, bool hideHudNotification)
    {
        Item? actualItem = mergedIntoStack ?? item;
        if (actualItem is { QualifiedItemId: $"(BC){ModKeys.CAFE_SIGNBOARD_OBJECT_ID}" })
        {
            actualItem.specialItem = true;
        }
    }

    private static bool Before_NpcCheckAction(NPC __instance, Farmer who, GameLocation l, ref bool __result)
    {
        if ((Mod.Cafe.NpcCustomers.Contains(__instance.Name) || __instance.Name.StartsWith(ModKeys.CUSTOMER_NPC_NAME_PREFIX))
            && (l.Equals(Mod.Cafe.Signboard?.Location)))
        {
            Table? table = Mod.Cafe.GetTableFromCustomer(__instance);
            
            if (table is { State.Value: TableState.CustomersWaitingForFood } && Cafe.InteractWithTable(table, who))
            {
                __result = true;
                return false;
            }
        }

        return true;
    }

    private static bool Before_GameLocationCheckAction(GameLocation __instance, Location tileLocation, Rectangle viewport, Farmer who, ref bool __result)
    {
        if ((__instance.Equals(Mod.Cafe.Signboard?.Location)))
        {
            Table? table = Mod.Cafe.GetTableAt(__instance, new Point(tileLocation.X, tileLocation.Y));

            if (table != null && Cafe.InteractWithTable(table, who))
            {
                __result = true;
                return false;
            }
        }

        return true;
    }
}
