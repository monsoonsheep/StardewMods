using System.Diagnostics;
using StardewValley.Pathfinding;
using StardewValley;
using Microsoft.Xna.Framework;
using StardewMods.VisitorsMod.Framework.Services.Visitors;

namespace StardewMods.VisitorsMod.Framework.Visitors;
internal class RoadSpawner : LocationSpawner, ISpawner
{
    public RoadSpawner(NpcMovement npcMovement) : base (npcMovement)
    {

    }

    public override string Id
        => "Road";

    protected override (GameLocation, Point) GetSpawnLocation()
    {
        return (Game1.getLocationFromName("BusStop"), new Point(40, 9));
    }
}
