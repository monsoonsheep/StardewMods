using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;

namespace StardewMods.FarmHelpers.Framework.Jobs;
internal class ItemCollectionJob : Job
{
    private int index = -1;
    private List<Point> pickupStandingPoints = [];
    private List<Point> pickupObjectPoints = [];

    private Func<StardewValley.Object, bool> toCollect;

    internal ItemCollectionJob(Func<StardewValley.Object, bool> toCollect, Action<Job>? onFinish, GameLocation location, Point? startingPosition, NPC npc) : base(npc, location, onFinish)
    {
        startPoint = startingPosition;
        this.toCollect = toCollect;
    }

    internal override void Start(NPC npc)
    {
        Log.Debug("Starting Item collection job");

        List<Point> pickupsUnordered = [];

        foreach (StardewValley.Object obj in this.location.Objects.Values)
        {
            if (this.toCollect(obj))
            {
                pickupsUnordered.Add(obj.TileLocation.ToPoint());
            }
        }

        this.pickupObjectPoints = ModUtility.GetNaturalPath(StartPoint, pickupsUnordered);
        List<StardewValley.Object> pickupObjects = this.pickupObjectPoints.Select(p => this.location.Objects[p.ToVector2()]).ToList();

        // Chain-Path to the tile next to each pickup
        Dictionary<Point, StardewValley.Object> temporaryPickupObjects = [];
        Point current = StartPoint;
        for (int i = 0; i < this.pickupObjectPoints.Count; i++)
        {
            Point? nearestEmpty = Mod.Pathfinding.FindPathToNearestEmptyTileNextToTarget(this.location, current, this.pickupObjectPoints[i], this.npc)?.LastOrDefault();
            if (!nearestEmpty.HasValue)
            {
                Log.Error("Can't path to coop object");
                continue;
            }

            // temporarily remove the object to make way for future pathing
            StardewValley.Object obj = this.location.Objects[this.pickupObjectPoints[i].ToVector2()];
            this.location.Objects.Remove(this.pickupObjectPoints[i].ToVector2());
            temporaryPickupObjects.Add(this.pickupObjectPoints[i], obj);

            this.pickupStandingPoints.Add(nearestEmpty.Value);

            current = nearestEmpty.Value;
        }

        // restore all the removed objects
        foreach (var pair in temporaryPickupObjects)
        {
            this.location.Objects.Add(pair.Key.ToVector2(), pair.Value);
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

        Log.Debug($"{this.pickupObjectPoints.Count} objects to collect");

        // start moving
        this.MoveToNextPickup();
    }

    private void MoveToNextPickup()
    {
        this.index += 1;

        if (this.index >= this.pickupObjectPoints.Count)
        {
            Log.Debug("All objects collected");

            base.Finish(npc);
            return;
        }

        Point target = this.pickupStandingPoints[this.index];
        Mod.Movement.Move(this.location, target, this.PickupObject);
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

        StardewValley.Object obj = this.location.removeObject(objectTile.ToVector2(), showDestroyedObject: false);

        Log.Debug($"Picking up '{obj.DisplayName}'");

        Mod.HelperInventory.Add(obj);
    }

    internal static bool IsAvailable(GameLocation location)
    {
        return location.Objects.Values.Any(o => ModUtility.IsCollectableObject(o));
    }
}
