using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using StardewMods.SheepCore.Framework.Services;
using StardewValley;
using StardewValley.Buildings;

namespace StardewMods.FarmHelpers.Framework;
internal class CoopJob : Job
{
    private readonly Building coop;
    private Point indoorEntry;

    private ItemCollectionJob itemCollection = null!;
    private TroughJob trough = null!;

    internal CoopJob(NPC npc, Building coop, Action<Job> onFinish) : base(npc, coop.GetIndoors(), onFinish)
    {
        this.coop = coop;
        this.StartPoint = coop.getPointForHumanDoor() + new Point(0, 1);
    }

    internal override void Start(NPC npc)
    {
        Log.Trace("Starting coop job");

        this.indoorEntry = HelperManager.EnterBuilding(this.npc, this.coop);

        this.itemCollection = new ItemCollectionJob(ModUtility.IsCollectableObject, this.OnFinishCollecting, base.location, this.indoorEntry, this.npc);
        this.trough = new TroughJob(this.npc, base.location, this.OnFinishTrough);

        // Could do MoveTo StartPoint, then Start, but he's already at StartPoint so just start
        this.itemCollection.Start(npc);
    }

    internal void OnFinishCollecting(Job job)
    {
        HelperManager.MoveHelper(this.location, this.trough.StartPoint, this.trough.Start);
    }

    internal void OnFinishTrough(Job job)
    {
        HelperManager.MoveHelper(this.location, this.StartPoint, (n) => this.onFinish(this));
    }

    internal static bool IsAvailable(Building coop)
    {
        return coop.GetIndoors() is AnimalHouse house
            && house.Objects.Values.Any(o => ModUtility.IsCollectableObject(o))
            && TroughJob.IsAvailable(house);
    }
}
