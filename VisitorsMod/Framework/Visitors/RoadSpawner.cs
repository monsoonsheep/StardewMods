using Microsoft.Xna.Framework;
using StardewMods.VisitorsMod.Framework.Data;
using StardewMods.SheepCore.Framework.Services;

namespace StardewMods.VisitorsMod.Framework.Visitors;
internal class RoadSpawner : LocationSpawner, ISpawner
{
    public RoadSpawner(NpcMovement npcMovement) : base (npcMovement)
    {

    }

    public override string Id
        => "Road";

    protected override (GameLocation, Point) GetSpawnLocation(Visit visit)
    {
        return (Game1.getLocationFromName("BusStop"), new Point(40, 9));
    }
}