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
internal class CoopJob : CompositeJob
{
    private readonly Building coop;
    private Point indoorEntry;

    private ItemCollectionJob? itemCollection = null!;
    private TroughJob? trough = null!;

    internal CoopJob(NPC npc, Building coop, Action<Job> onFinish) : base(npc, coop.GetIndoors(), onFinish)
    {
        this.coop = coop;
        this.StartPoint = coop.getPointForHumanDoor() + new Point(0, 1);
    }

    /// <summary>
    /// Start job when at the door of the coop
    /// </summary>
    internal override void Start(NPC npc)
    {
        Log.Debug("Starting coop job");

        this.indoorEntry = HelperManager.EnterBuilding(this.npc, this.coop);

        if (ItemCollectionJob.IsAvailable(this.location))
        {
            base.subJobs.Add(new ItemCollectionJob(
                ModUtility.IsCollectableObject,
                null,
                base.location,
                this.indoorEntry,
                this.npc));
        }

        if (TroughJob.IsAvailable(this.location))
        {
            base.subJobs.Add(new TroughJob(this.npc, base.location, null));
        }

        if (PettingJob.IsAvailable(this.location))
        {
            base.subJobs.Add(new PettingJob(this.npc, base.location, null, null));
        }

        base.Start(npc);
    }

    internal override void DoneAllJobs()
    {
        Log.Debug("All sub-jobs done for coop job");

        HelperManager.MoveHelper(this.location, this.indoorEntry, this.Finish);
    }

    internal override void Finish(NPC npc)
    {
        Log.Debug("Leaving coop");

        HelperManager.ExitBuilding(npc, this.location, this.StartPoint);
        if (ModUtility.IsTimeForOpeningAnimalDoors(this.coop.GetParentLocation()))
        {
            this.coop.animalDoorOpen.Set(true);
        }
        base.Finish(npc);
    }

    internal static bool IsAvailable(Building coop)
    {
        return coop.GetIndoors() is AnimalHouse house
            && (ItemCollectionJob.IsAvailable(house) || TroughJob.IsAvailable(house) || PettingJob.IsAvailable(house));
    }
}
