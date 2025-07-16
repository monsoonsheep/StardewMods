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

    internal abstract void Start(NPC npc);

    protected Point EnterBuilding(Building building)
    {
        GameLocation indoors = building.GetIndoors();

        Point entry = ModUtility.GetEntryTileForBuildingIndoors(building);

        Game1.warpCharacter(this.helper, indoors, entry.ToVector2());

        return entry;
    }
}
