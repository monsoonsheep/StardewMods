using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.Tools;

namespace StardewMods.FarmHelpers.Framework;
internal class BarnJob : Job
{
    private readonly Building barn;

    private MilkPail milkPail = null!;

    internal BarnJob(NPC helper, Building barn, Action<Job> onFinish) : base(helper, barn.GetIndoors(), onFinish)
    {
        this.barn = barn;

        this.StartPoint = barn.getPointForHumanDoor() + new Point(0, 1);

        this.milkPail = new MilkPail();
    }

    internal override void Start(NPC npc)
    {

    }

    protected override void ResetAndFinish(NPC npc)
    {
        throw new NotImplementedException();
    }
}
