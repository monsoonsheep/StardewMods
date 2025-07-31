using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.Tools;

namespace StardewMods.FarmHelpers.Framework.Jobs;
internal class BarnJob : CompositeJob
{
    private readonly Building barn;

    private Point indoorEntry;

    private MilkPail milkPail = null!;

    internal BarnJob(NPC helper, Building barn, Action<Job>? onFinish) : base(helper, barn.GetIndoors(), onFinish)
    {
        this.barn = barn;

        this.StartPoint = barn.getPointForHumanDoor() + new Point(0, 1);

        this.milkPail = new MilkPail();
    }

    internal override void Start(NPC npc)
    {
        Log.Debug("Starting barn job");

        this.indoorEntry = Mod.Movement.EnterBuilding(this.barn);

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

        if (AnimalProduceJob.IsAvailable(this.location))
        {
            subJobs.Add(new AnimalProduceJob(this.npc, location, null, null));
        }

        base.Start(npc);
    }

    internal override void DoneAllJobs()
    {
        Log.Debug("All sub-jobs done for barn job");

        Mod.Movement.Move(this.location, this.indoorEntry, this.Finish);
    }

    internal override void Finish(NPC npc)
    {
        Log.Debug("Leaving barn");

        Mod.Movement.ExitBuilding(this.location, this.StartPoint);
        if (ModUtility.IsTimeForOpeningAnimalDoors(this.barn.GetParentLocation()))
        {
            this.barn.animalDoorOpen.Set(true);
        }
        base.Finish(npc);
    }

    internal static bool IsAvailable(Building barn)
    {
        return barn.GetIndoors() is AnimalHouse house
            && (ItemCollectionJob.IsAvailable(house) || TroughJob.IsAvailable(house) || PettingJob.IsAvailable(house));
    }
}
