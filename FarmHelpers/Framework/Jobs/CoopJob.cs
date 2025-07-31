using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using StardewMods.SheepCore.Framework.Services;
using StardewValley;
using StardewValley.Buildings;

namespace StardewMods.FarmHelpers.Framework.Jobs;
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

        this.indoorEntry = Mod.Movement.EnterBuilding(this.coop);

        if (ItemCollectionJob.IsAvailable(this.location))
        {
            subJobs.Add(new ItemCollectionJob(
                ModUtility.IsCollectableObject,
                null,
                location,
                this.indoorEntry,
                this.npc));
        }

        if (TroughJob.IsAvailable(this.location))
        {
            subJobs.Add(new TroughJob(this.npc, location, null));
        }

        if (PettingJob.IsAvailable(this.location))
        {
            subJobs.Add(new PettingJob(this.npc, location, null, null));
        }

        base.Start(npc);
    }

    internal override void DoneAllJobs()
    {
        Log.Debug("All sub-jobs done for coop job");

        Mod.Movement.Move(this.location, this.indoorEntry, this.Finish);
    }

    internal override void Finish(NPC npc)
    {
        Log.Debug("Leaving coop");

        Mod.Movement.ExitBuilding(this.location, this.StartPoint);
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
