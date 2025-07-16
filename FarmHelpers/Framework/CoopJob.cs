using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Buildings;

namespace StardewMods.FarmHelpers.Framework;
internal class CoopJob : Job
{
    private readonly Building coop;
    private readonly GameLocation indoors;

    internal CoopJob(NPC helper, Building coop)
    {
        base.helper = helper;
        this.coop = coop;
        this.indoors = coop.GetIndoors();

        this.StartPoint = coop.getPointForHumanDoor();

        
    }

    internal override void Start(NPC npc)
    {
        Point entry = base.EnterBuilding(this.coop);

        List<Point> pickups = [];
        List<StardewValley.Object> pickupObjects = [];

        foreach (StardewValley.Object obj in this.indoors.Objects.Values)
        {
            if (IsCollectibleObject(obj))
            {
                pickups.Add(obj.TileLocation.ToPoint());
                pickupObjects.Add(obj);
            }
        }

        List<Point> path = ModUtility.GetNaturalPath(entry, pickups);

        List<Point> pickupPoints = [];

        Dictionary<Point, StardewValley.Object> temporaryPickupObjects = [];

        Point current = entry;

        for (int i = 0; i < pickups.Count; i++)
        {
            Point? nearestEmpty = Mod.Pathfinding.FindPathToNearestEmptyTileNextToTarget(this.indoors, current, pickups[i], npc)?.LastOrDefault();
            if (!nearestEmpty.HasValue)
            {
                Log.Error("Can't path to coop object");
                continue;
            }

            // temporarily remove the object to make way for future pathing
            StardewValley.Object obj = this.indoors.Objects[pickups[i].ToVector2()];
            this.indoors.Objects.Remove(pickups[i].ToVector2());
            temporaryPickupObjects.Add(pickups[i], obj);


            pickupPoints.Add(nearestEmpty.Value);

            current = nearestEmpty.Value;
        }

        // restore all the removed objects
        foreach (var pair in temporaryPickupObjects)
        {
            this.indoors.Objects.Add(pair.Key.ToVector2(), pair.Value);
        }


    }

    private void PickupObject(Point tile)
    {

    }

    internal static bool IsAvailable(Building coop)
    {
        foreach (StardewValley.Object obj in coop.GetIndoors().Objects.Values)
        {
            if (IsCollectibleObject(obj))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsCollectibleObject(StardewValley.Object obj)
    {
        return (obj.QualifiedItemId == "(O)444" || obj.HasContextTag("egg_item") || obj.HasContextTag("(O)446") || obj.HasContextTag("(O)440"));
    }
}
