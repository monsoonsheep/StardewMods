using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StardewValley.Locations;

namespace StardewMods.FarmHelpers.Framework;
internal class ItachiHouseFixes
{
    internal static bool BushesRemoved = false;

    internal static void RemoveBushes()
    {
        // Remove the bush blocking the way to helper's house
        GameLocation forest = Game1.RequireLocation<Forest>("Forest");

        var bush = forest.getLargeTerrainFeatureAt(68, 83);
        forest.largeTerrainFeatures.Remove(bush);

        BushesRemoved = true;
    }
}
