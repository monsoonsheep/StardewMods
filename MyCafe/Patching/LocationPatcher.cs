using System.Diagnostics.CodeAnalysis;
using HarmonyLib;
using Microsoft.Xna.Framework;
using MonsoonSheep.Stardew.Common.Patching;
using Monsoonsheep.StardewMods.MyCafe.Characters;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Buildings;
using xTile.Layers;
using xTile.Tiles;

namespace Monsoonsheep.StardewMods.MyCafe.Patching;

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

    /// <summary>
    /// For pathfinding, when NPCs try to path to a building on the farm, the game checks for the interior's uniqueName instead of the Name
    /// We replace it with Name
    /// </summary>
    private static void After_GetWarpPointTo(GameLocation __instance, string location, Character? character, ref Point __result)
    {
        if (__result == Point.Zero && location.Contains(ModKeys.CAFE_MAP_NAME))
        {
            // Verify what the above name is and maybe make it constant
            Building? b = __instance.getBuildingByType(ModKeys.CAFE_BUILDING);
            if (b != null && b.GetIndoors()?.Name.Equals(location) == true && !b.GetIndoors().uniqueName.Value.Equals(location))
            {
                __result = __instance.getWarpPointTo(b.GetIndoors().uniqueName.Value, character);
            }
        }
    }

    /// <summary>
    /// For pathfinding, when NPCs try to path to a building on the farm, the game checks for the interior's uniqueName instead of the Name
    /// We replace it with Name
    /// </summary>
    private static void After_BuildingHasIndoorsName(Building __instance, string name, ref bool __result)
    {
        // Verify what the above name is and maybe make it constant
        if (Context.IsMainPlayer
            && __result == false
            && (name == Mod.Cafe.Signboard?.Location.Name)
            && __instance.indoors.Value?.Name == name)
        {
            __result = true;
        }
    }

    private static void After_GameLocationLoadMap(GameLocation __instance, string mapPath, bool force_reload)
    {
        if (__instance is Farm)
        {
            Pathfinding.NpcBarrierTiles.Clear();

            Layer layer = __instance.Map.GetLayer("Back");

            for (int i = 0; i < layer.LayerWidth; i++)
            {
                for (int j = 0; j < layer.LayerHeight; j++)
                {
                    Tile tile = layer.Tiles[i, j];
                    if (tile == null)
                        continue;

                    if (tile.Properties.ContainsKey("NPCBarrier"))
                    {
                        Pathfinding.NpcBarrierTiles.Add(new Point(i, j));
                    }
                }
            }
        
        }
    }
}
