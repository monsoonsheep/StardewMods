using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Buildings;

namespace StardewMods.FarmHelpers.Framework;
internal class BarnJob : Job
{
    private readonly Building barn;
    private readonly GameLocation indoors;
    private Point entry;

    internal BarnJob(NPC helper, Building barn) : base(helper)
    {
        this.barn = barn;
        this.indoors = barn.GetIndoors();

        this.StartPoint = barn.getPointForHumanDoor() + new Point(0, 1);
    }

    internal override void Start(NPC npc)
    {

    }

    protected override void Finish(NPC npc)
    {
        throw new NotImplementedException();
    }

}
