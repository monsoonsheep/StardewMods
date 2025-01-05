using StardewMods.FoodJoints.Framework.Enums;
using StardewMods.FoodJoints.Framework.Objects;
using StardewMods.FoodJoints.Framework.UI;
using xTile.Dimensions;

namespace StardewMods.FoodJoints.Framework.Services;
internal class ActionPatches
{
    internal static ActionPatches Instance = null!;

    internal ActionPatches()
        => Instance = this;

    internal void Initialize()
    {
        Harmony harmony = Mod.Harmony;
        
        harmony.Patch(
            original: AccessTools.Method(typeof(StardewValley.Object), nameof(StardewValley.Object.performRemoveAction)),
            postfix: new HarmonyMethod(AccessTools.Method(this.GetType(), nameof(ActionPatches.After_ObjectPerformRemoveAction)))
        );
        harmony.Patch(
            original: AccessTools.Method(typeof(Farmer), (nameof(Farmer.OnItemReceived)), [typeof(Item), typeof(int), typeof(Item), typeof(bool)]),
            postfix: new HarmonyMethod(AccessTools.Method(this.GetType(), nameof(ActionPatches.After_FarmerOnItemReceived)))
        );
        harmony.Patch(
            original: AccessTools.Method(typeof(StardewValley.Object), nameof(StardewValley.Object.placementAction)),
            postfix: new HarmonyMethod(AccessTools.Method(this.GetType(), nameof(ActionPatches.After_ObjectPlacementAction)))
        );
        harmony.Patch(
            original: AccessTools.Method(typeof(StardewValley.Object), nameof(StardewValley.Object.checkForAction), [typeof(Farmer), typeof(bool)]),
            postfix: new HarmonyMethod(AccessTools.Method(this.GetType(), nameof(ActionPatches.After_ObjectCheckForAction)))
        );
        harmony.Patch(
            original: AccessTools.Method(typeof(GameLocation), nameof(GameLocation.checkAction)),
            prefix: new HarmonyMethod(AccessTools.Method(this.GetType(), nameof(ActionPatches.Before_GameLocationCheckAction)))
        );
        harmony.Patch(
            original: AccessTools.Method(typeof(NPC), nameof(NPC.checkAction)),
            prefix: new HarmonyMethod(AccessTools.Method(this.GetType(), nameof(ActionPatches.Before_NpcCheckAction)))
        );
    }

    private static void After_ObjectCheckForAction(StardewValley.Object __instance, Farmer who, bool justCheckingForActivity, ref bool __result)
    {
        // Signboard
        if (__instance.QualifiedItemId.Equals($"(BC){Values.CAFE_SIGNBOARD_OBJECT_ID}") && !justCheckingForActivity && __result == false)
        {
            if (Game1.activeClickableMenu == null)
            {
                Game1.activeClickableMenu = new CafeMenu();
            }
        }
    }

    private static void After_ObjectPlacementAction(StardewValley.Object __instance, GameLocation location, int x, int y, Farmer who, ref bool __result)
    {
        // Signboard
        if (__instance is { QualifiedItemId: $"(BC){Values.CAFE_SIGNBOARD_OBJECT_ID}" })
        {
            Log.Trace("Placed down signboard");
            Mod.Locations.OnPlacedDownSignboard(__instance);
        }
    }

    private static void After_ObjectPerformRemoveAction(StardewValley.Object __instance)
    {
        // Signboard
        if (__instance is { QualifiedItemId: $"(BC){Values.CAFE_SIGNBOARD_OBJECT_ID}" })
        {
            Log.Trace("Removed signboard");
            Mod.Locations.OnRemovedSignboard(__instance);
        }
    }

    private static void After_FarmerOnItemReceived(Farmer __instance, Item? item, int countAdded, Item? mergedIntoStack, bool hideHudNotification)
    {
        // Signboard
        Item? actualItem = mergedIntoStack ?? item;
        if (actualItem is { QualifiedItemId: $"(BC){Values.CAFE_SIGNBOARD_OBJECT_ID}" })
        {
            actualItem.specialItem = true;
        }
    }

    private static bool Before_NpcCheckAction(NPC __instance, Farmer who, GameLocation l, ref bool __result)
    {
        if (Mod.Customers.Groups.SelectMany(g => g.Members).Any(n => n == __instance) && (l.Equals(Mod.Locations.Signboard?.Location)))
        {
            Table? table = Mod.Tables.GetTableFromCustomer(__instance);

            if (table is { State.Value: TableState.CustomersWaitingForFood } && Mod.Tables.InteractWithTable(table, who))
            {
                __result = true;
                return false;
            }
        }

        return true;
    }

    private static bool Before_GameLocationCheckAction(GameLocation __instance, Location tileLocation, xTile.Dimensions.Rectangle viewport, Farmer who, ref bool __result)
    {
        if ((__instance.Equals(Mod.Locations.Signboard?.Location)))
        {
            Table? table = Mod.Tables.GetTableAt(__instance, new Point(tileLocation.X, tileLocation.Y));

            if (table != null && Mod.Tables.InteractWithTable(table, who))
            {
                __result = true;
                return false;
            }
        }

        return true;
    }
}
