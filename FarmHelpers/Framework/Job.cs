using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using StardewMods.SheepCore.Framework.Services;
using StardewValley.Buildings;

namespace StardewMods.FarmHelpers.Framework;
internal abstract class Job
{
    protected NPC npc = null!;

    internal Point StartPoint = new Point (-9999, -9999);
    internal GameLocation location;
    internal Action<Job> onFinish;

    protected Job(NPC npc, GameLocation location, Action<Job> onFinish)
    {
        this.npc = npc;
        this.location = location;
        this.onFinish = onFinish;
    }

    internal abstract void Start(NPC npc);
}
