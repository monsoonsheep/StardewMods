using System.Diagnostics.CodeAnalysis;
using HarmonyLib;
using Microsoft.Xna.Framework;
using MonsoonSheep.Stardew.Common.Patching;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Buildings;
using xTile.Layers;
using xTile.Tiles;

namespace MyCafe.Patching;

[SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Harmony patching requirement")]
internal class LocationPatcher : BasePatcher
{
    public override void Apply(Harmony harmony, IMonitor monitor)
    {
        harmony.Patch(
            original: this.RequireMethod<GameLocation>(nameof(GameLocation.getWarpPointTo), [typeof(string), typeof(Character)]),
            postfix: this.GetHarmonyMethod(nameof(LocationPatcher.After_GetWarpPointTo))
        );
        harmony.Patch(
            original: this.RequireMethod<Building>(nameof(Building.HasIndoorsName), [typeof(string)]),
            postfix: this.GetHarmonyMethod(nameof(LocationPatcher.After_BuildingHasIndoorsName))
        );
        harmony.Patch(
            original: this.RequireMethod<GameLocation>(nameof(GameLocation.loadMap), [typeof(string), typeof(bool)]),
            postfix: this.GetHarmonyMethod(nameof(LocationPatcher.After_GameLocationLoadMap))
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

    private static void After_BuildingHasIndoorsName(Building __instance, string name, ref bool __result)
    {
        if (Context.IsMainPlayer && __result == false && name == Mod.Cafe.Indoor?.Name && __instance.indoors.Value?.Name == name)
        {
            __result = true;
        }
    }

    private static void After_GameLocationLoadMap(GameLocation __instance, string mapPath, bool force_reload)
    {
        if (__instance is Farm)
        {
            Layer layer = __instance.Map.GetLayer("Back");

            for (int i = 0; i < layer.LayerWidth; i++)
            {
                for (int j = 0; j < layer.LayerHeight; j++)
                {
                    Tile tile = layer.Tiles[i, j];
                    if (tile == null)
                        continue;

                    tile.Properties.Remove("NPCBarrier");
                    tile.TileIndexProperties.Remove("NPCBarrier");
                }
            }
        
        }
    }
}
