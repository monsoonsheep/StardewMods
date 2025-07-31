using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StardewMods.FarmHelpers.Framework.Jobs;
internal abstract class CompositeJob : Job
{
    protected List<Job> subJobs = [];
    protected int index = -1;

    internal CompositeJob(NPC npc, GameLocation location, Action<Job>? onFinish) : base(npc, location, onFinish)
    {

    }

    internal override void Start(NPC npc)
    {
        this.NextJob();
    }

    internal void NextJob()
    {
        this.index += 1;

        if (this.index >= this.subJobs.Count)
        {
            this.DoneAllJobs();
            return;
        }
        
        Job nextJob = this.subJobs[this.index];
        nextJob.onFinish = this.OnSubJobFinished;

        if (this.npc.TilePoint.X == nextJob.StartPoint.X && this.npc.TilePoint.Y == nextJob.StartPoint.Y)
        {
            nextJob.Start(this.npc);
        }
        else
        {
            Mod.Movement.Move(this.location, nextJob.StartPoint, nextJob.Start);
        }
    }

    internal abstract void DoneAllJobs();

    internal void OnSubJobFinished(Job job)
    {
        this.NextJob();
    }
}
