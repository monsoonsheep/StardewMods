using StardewMods.VisitorsMod.Framework.Data.Models.Activities;
using StardewMods.VisitorsMod.Framework.Enums;
using StardewMods.VisitorsMod.Framework.Interfaces;

namespace StardewMods.VisitorsMod.Framework.Data;
public class Visit
{
    internal VisitState state = VisitState.NotStarted;
    internal List<NPC> group = [];
    internal ActivityModel activity;
    internal ISpawner spawner;
    internal int startTime;
    internal int endTime;

    public Visit(ActivityModel activity, ISpawner spawner, int startTime, int endTime)
    {
        this.activity = activity;
        this.spawner = spawner;
        this.startTime = startTime;
        this.endTime = endTime;
    }
}
