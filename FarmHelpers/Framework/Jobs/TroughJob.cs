using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using xTile;
using xTile.Tiles;

namespace StardewMods.FarmHelpers.Framework.Jobs;
internal class TroughJob : Job
{
    private List<StardewValley.Object> hays = [];
    private List<Point> emptySlots = [];
    private int index = -1;
    private StardewValley.Object hopper;

    internal TroughJob(NPC npc, GameLocation location, Action<Job>? onFinish) : base(npc, location, onFinish)
    {
        StardewValley.Object? h = this.location.Objects.Values.FirstOrDefault(o => o.Name.Equals("Feed Hopper"))
            ?? throw new Exception("Hopper not found in animal house");

        this.hopper = h;

        foreach (Point p in ModUtility.GetEmptyTilesNextTo(this.location, this.hopper.TileLocation.ToPoint(), directionToPrioritize: 2))
        {
            StartPoint = p;
            break;
        }

        if (StartPoint.X == -9999)
        {
            // INVALID
            return;
        }
    }

    internal override void Start(NPC npc)
    {
        Log.Debug("Starting trough job");
        
        GameLocation rootLocation = this.location.GetRootLocation();
        Map map = rootLocation.Map;

        foreach (Point p in EmptyTroughSlots(this.location))
        {
            StardewValley.Object hay = GameLocation.GetHayFromAnySilo(rootLocation);

            if (hay == null)
            {
                // go on to place the hay you have, or go home
                break;
            }

            this.emptySlots.Add(p);
            this.hays.Add(hay);
        }

        Log.Debug($"Collected {this.hays.Count} hays from hopper");

        this.npc.faceDirection(Utility.getDirectionFromChange(this.hopper.TileLocation, this.npc.Tile));

        ModUtility.AddDelayedAction(() => this.GoToPlaceNextHay(this.npc), Game1.random.Next(200, 800));
    }

    private void GoToPlaceNextHay(NPC npc)
    {
        this.index += 1;

        if (this.index >= this.emptySlots.Count)
        {
            Log.Debug("All trough hays done");

            base.Finish(npc);
            return;
        }

        Point haySlot = this.emptySlots[this.index];
        Point? standingTile = ModUtility.GetEmptyTilesNextTo(this.location, haySlot, directionToPrioritize: 2).FirstOrDefault();

        if (!standingTile.HasValue)
        {
            Log.Error("Trough is blocked!");

            this.GoToPlaceNextHay(npc);
            return;
        }

        Mod.Movement.Move(this.location, standingTile.Value, this.PlaceHay);
    }

    private void PlaceHay(NPC npc)
    {
        Point haySlot = this.emptySlots[this.index];

        this.npc.faceDirection(Utility.getDirectionFromChange(haySlot.ToVector2(), this.npc.Tile));

        this.location.objects.Add(haySlot.ToVector2(), this.hays[this.index]);

        ModUtility.AddDelayedAction(() => this.GoToPlaceNextHay(this.npc), Game1.random.Next(200, 800));
    }

    private static IEnumerable<Point> EmptyTroughSlots(GameLocation location)
    {
        Map map = location.Map;

        for (int x = 0; x < map.Layers[0].LayerWidth; x++)
        {
            for (int y = 0; y < map.Layers[0].LayerHeight; y++)
            {
                if (location.doesTileHaveProperty(x, y, "Trough", "Back") != null)
                {
                    Vector2 tile = new Vector2(x, y);

                    if (!location.Objects.ContainsKey(tile))
                    {
                        yield return new Point(x, y);
                    }
                }
            }
        }
    }

    internal static bool IsAvailable(GameLocation location)
    {
        return EmptyTroughSlots(location).Any();
    }
}
