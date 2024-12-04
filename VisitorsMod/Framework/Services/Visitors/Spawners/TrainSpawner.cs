using Microsoft.Xna.Framework;
using StardewMods.VisitorsMod.Framework.Data;
using StardewMods.SheepCore.Framework.Services;
using StardewMods.VisitorsMod.Framework.Interfaces;

namespace StardewMods.VisitorsMod.Framework.Services.Visitors.Spawners;
internal class TrainSpawner : LocationSpawner, ISpawner
{
    public override bool IsAvailable()
    {
        return Game1.stats.DaysPlayed >= 31;
    }

    public override string Id
        => "Train";

    protected override (GameLocation, Point) GetSpawnLocation(Visit visit)
    {
        return (Game1.getLocationFromName("Railroad"), new Point(30, 40));
    }
}
