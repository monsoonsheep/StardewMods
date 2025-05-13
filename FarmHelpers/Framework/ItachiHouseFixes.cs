using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StardewValley.Locations;

namespace StardewMods.FarmHelpers.Framework;
internal class ItachiHouseFixes
{
    internal bool BushesRemoved = false;

    internal ItachiHouseFixes()
    {
        Mod.Events.GameLoop.DayStarted += this.OnDayStarted;
    }

    private void OnDayStarted(object? sender, DayStartedEventArgs e)
    {
        if (Game1.characterData.ContainsKey($"{Mod.Manifest.UniqueID}_Itachi") && this.BushesRemoved == false)
        {
            this.RemoveBushes();
        }
    }

    internal void RemoveBushes()
    {
        // Remove the bush blocking the way to helper's house
        GameLocation forest = Game1.RequireLocation<Forest>("Forest");

        var bush = forest.getLargeTerrainFeatureAt(68, 83);
        forest.largeTerrainFeatures.Remove(bush);

        this.BushesRemoved = true;
    }
}
