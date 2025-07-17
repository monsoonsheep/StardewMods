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
    protected NPC helper = null!;

    internal Point StartPoint;

    protected Job(NPC helper)
    {
        this.helper = helper;
    }

    internal abstract void Start(NPC npc);

    protected virtual void Finish(NPC npc)
    {
        Log.Trace($"{this.GetType().FullName} job finished");

        if (npc.currentLocation != Mod.Locations.Farm || npc.TilePoint != this.StartPoint)
        {
            Game1.warpCharacter(npc, Mod.Locations.Farm, this.StartPoint.ToVector2());
        }

        HelperManager.OnFinishJob(this, npc);
    }
}
