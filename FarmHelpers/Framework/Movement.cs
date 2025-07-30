using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StardewMods.SheepCore.Framework.Services;
using StardewValley.Buildings;

namespace StardewMods.FarmHelpers.Framework;
internal class Movement
{
    internal bool Move(GameLocation location, Point tile, Action<NPC>? endBehavior)
    {
        NPC npc = Mod.Worker.npc ?? throw new Exception("worker NPC is null");

        // open all closed fences
        bool isFarm = location is Farm;

        isFarm = false;

        List<Fence> fences = [];
        if (isFarm)
        {
            Log.Debug("Checking fences and opening");

            foreach (StardewValley.Object obj in Mod.Locations.Farm.Objects.Values)
            {
                if (obj is Fence fence && fence.isGate.Value && fence.gatePosition.Value == 0)
                {
                    fence.toggleGate(true, is_toggling_counterpart: true);
                    fences.Add(fence);
                }
            }
        }

        bool res = false;

        Stack<Point>? path = Mod.Pathfinding.PathfindFromLocationToLocation(npc.currentLocation, npc.TilePoint, location, tile, null);
        if (path?.Any() == true)
        {
            npc.MoveTo(path, endBehavior);
            res = true;
        }

        // close the fences again
        if (res == true && isFarm)
        {
            Log.Debug("Closing fences back");

            foreach (Fence fence in fences)
            {
                fence.toggleGate(false, is_toggling_counterpart: true);
            }
        }

        // set isCharging to true because of collisions with farm animals
        if (ModUtility.IsFarmOrIndoor(location) && location.animals.FieldDict.Count > 0)
        {
            Log.Debug("Enabling charging for NPC");
            npc.isCharging = true;
        }

        return res;
    }

    internal static Point EnterBuilding(NPC npc, Building building)
    {
        GameLocation indoors = building.GetIndoors();

        Point entry = ModUtility.GetEntryTileForBuildingIndoors(building);

        Game1.warpCharacter(npc, indoors, entry.ToVector2());

        return entry;
    }

    internal static Point ExitBuilding(NPC npc, GameLocation indoors, Point? exitPointOnFarm = null)
    {
        GameLocation parentLocation = indoors.GetParentLocation();
        Building building = indoors.ParentBuilding;

        if (exitPointOnFarm == null)
        {
            exitPointOnFarm = building.getPointForHumanDoor();
        }

        Game1.warpCharacter(npc, parentLocation, exitPointOnFarm.Value.ToVector2());

        return exitPointOnFarm.Value;
    }
}
