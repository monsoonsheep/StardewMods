using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using StardewMods.SheepCore.Framework.Services;
using StardewValley;
using StardewValley.Buildings;

namespace StardewMods.FarmHelpers.Framework;
internal class CoopJob : Job
{
    private readonly Building coop;
    private readonly GameLocation indoors;
    private Point entry;

    private int index = -1;
    private List<Point> pickupStandingPoints = [];
    private List<Point> pickupObjectPoints = [];


    internal CoopJob(NPC helper, Building coop) : base(helper)
    {
        this.coop = coop;
        this.indoors = coop.GetIndoors();

        this.StartPoint = coop.getPointForHumanDoor() + new Point(0, 1);
    }

    internal override void Start(NPC npc)
    {
        Log.Trace("Starting coop job");

        this.entry = HelperManager.EnterBuilding(npc, this.coop);

        List<Point> pickupsUnordered = [];

        foreach (StardewValley.Object obj in this.indoors.Objects.Values)
        {
            if (IsCollectibleObject(obj))
            {
                pickupsUnordered.Add(obj.TileLocation.ToPoint());
            }
        }

        this.pickupObjectPoints = ModUtility.GetNaturalPath(this.entry, pickupsUnordered);
        List<StardewValley.Object> pickupObjects = this.pickupObjectPoints.Select(p => this.indoors.Objects[p.ToVector2()]).ToList();

        // Chain-Path to the tile next to each pickup
        Dictionary<Point, StardewValley.Object> temporaryPickupObjects = [];
        Point current = this.entry;
        for (int i = 0; i < this.pickupObjectPoints.Count; i++)
        {
            Point? nearestEmpty = Mod.Pathfinding.FindPathToNearestEmptyTileNextToTarget(this.indoors, current, this.pickupObjectPoints[i], npc)?.LastOrDefault();
            if (!nearestEmpty.HasValue)
            {
                Log.Error("Can't path to coop object");
                continue;
            }

            // temporarily remove the object to make way for future pathing
            StardewValley.Object obj = this.indoors.Objects[this.pickupObjectPoints[i].ToVector2()];
            this.indoors.Objects.Remove(this.pickupObjectPoints[i].ToVector2());
            temporaryPickupObjects.Add(this.pickupObjectPoints[i], obj);

            this.pickupStandingPoints.Add(nearestEmpty.Value);

            current = nearestEmpty.Value;
        }

        // restore all the removed objects
        foreach (var pair in temporaryPickupObjects)
        {
            this.indoors.Objects.Add(pair.Key.ToVector2(), pair.Value);
        }

        if (this.pickupObjectPoints.Count > this.pickupStandingPoints.Count)
        {
            foreach (Point p in this.pickupObjectPoints)
            {
                if (this.pickupStandingPoints.Contains(p))
                {
                    this.pickupObjectPoints.Remove(p);
                }
            }
        }

        // start moving
        this.MoveToNextPickup();
    }

    private void MoveToNextPickup()
    {
        this.index += 1;

        if (this.index >= this.pickupObjectPoints.Count)
        {
            Log.Trace("All coop objects collected");

            HelperManager.MoveHelper(this.indoors, this.entry, this.Finish);
            return;
        }

        Point standingTile = this.pickupStandingPoints[this.index];

        HelperManager.MoveHelper(this.indoors, standingTile, this.PickupObject);
    }

    private void PickupObject(NPC npc)
    {
        Point objectTile = this.pickupObjectPoints[this.index];
        int directionToFace = ModUtility.GetDirectionFromTiles(objectTile, npc.TilePoint);

        npc.faceDirection(directionToFace);

        ModUtility.AddDelayedAction(this.ActuallyPickupObject, Game1.random.Next(100, 650));
        ModUtility.AddDelayedAction(this.MoveToNextPickup, Game1.random.Next(650, 990));
    }

    public void ActuallyPickupObject()
    {
        Point objectTile = this.pickupObjectPoints[this.index];

        // TODO maybe do animation to pickup? (far future  probably)

        StardewValley.Object obj = this.indoors.removeObject(objectTile.ToVector2(), showDestroyedObject: false);

        Mod.HelperInventory.Add(obj);
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
