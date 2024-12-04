using Microsoft.Xna.Framework;
using StardewMods.VisitorsMod.Framework.Data;
using StardewMods.SheepCore.Framework.Services;
using StardewMods.VisitorsMod.Framework.Interfaces;

namespace StardewMods.VisitorsMod.Framework.Services.Visitors.Spawners;
internal class RoadSpawner : LocationSpawner, ISpawner
{
    public override string Id
        => "Road";

    protected override (GameLocation, Point) GetSpawnLocation(Visit visit)
    {
        return (Game1.getLocationFromName("BusStop"), new Point(40, 9));
    }
}
