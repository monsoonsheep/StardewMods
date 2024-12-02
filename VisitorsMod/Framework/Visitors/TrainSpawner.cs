using System.Diagnostics;
using StardewValley.Pathfinding;
using StardewValley;
using Microsoft.Xna.Framework;
using StardewMods.VisitorsMod.Framework.Services.Visitors;

namespace StardewMods.VisitorsMod.Framework.Visitors;
internal class TrainSpawner : LocationSpawner, ISpawner
{
    public TrainSpawner(NpcMovement npcMovement) : base (npcMovement)
    {

    }

    public override bool IsAvailable()
    {
        return Game1.stats.DaysPlayed >= 31;
    }

    public override string Id
        => "Train";

    protected override (GameLocation, Point) GetSpawnLocation()
    {
        return (Game1.getLocationFromName("Railroad"), new Point(30, 40));
    }
}
