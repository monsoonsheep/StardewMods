using HarmonyLib;
using MonsoonSheep.Stardew.Common.Patching;
using MyCafe.Locations.Objects;
using StardewModdingAPI;
using StardewValley;
using xTile.Dimensions;

namespace MyCafe.Patching;

internal class LocationPatcher : BasePatcher
{
    public override void Apply(Harmony harmony, IMonitor monitor)
    {
        harmony.Patch(
            original: this.RequireMethod<GameLocation>("checkAction"),
            postfix: this.GetHarmonyMethod(nameof(After_CheckAction))
            );
        harmony.Patch(
            original: this.RequireMethod<Farm>("initNetFields"),
            postfix: this.GetHarmonyMethod(nameof(After_InitNetFields))
            );
    }

    private static void After_InitNetFields(Farm instance)
    {
        instance.NetFields.AddField(instance.get_Cafe(), $"{Mod.UniqueId}.Cafe");
        Mod.Instance.NetCafe = instance.get_Cafe();
    }

    private static void After_CheckAction(Farm instance, Location tileLocation, Rectangle viewport, Farmer who, ref bool result)
    {
        if (result == true || (!instance.Equals(Mod.Cafe.Indoor) && !instance.Equals(Mod.Cafe.Outdoor)))
            return;

        foreach (Table table in Mod.Cafe.Tables)
        {
            if (table.BoundingBox.Value.Contains(tileLocation.X * 64, tileLocation.Y * 64)
                && Mod.Cafe.InteractWithTable(table, who))
            {
                result = true;
                return;
            }
        }
    }
}