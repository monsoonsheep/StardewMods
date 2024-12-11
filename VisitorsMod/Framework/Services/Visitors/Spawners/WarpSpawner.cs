using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewMods.VisitorsMod.Framework.Data;
using StardewMods.SheepCore.Framework.Services;
using StardewMods.VisitorsMod.Framework.Interfaces;
using StardewValley;

namespace StardewMods.VisitorsMod.Framework.Services.Visitors.Spawners;
internal class WarpSpawner : LocationSpawner, ISpawner
{
    public override string Id
        => "Warp";

    protected override (GameLocation, Point) GetSpawnLocation(Visit visit)
    {
        GameLocation location = Game1.getLocationFromName(visit.activity.Location);
        Warp warpOut = location.GetFirstPlayerWarp();
        GameLocation warpedLocation = Game1.getLocationFromName(warpOut.TargetName);

        Point entryPoint = Pathfinding.Instance.GetEntryPointIntoLocation(location, warpedLocation);

        return (location, entryPoint);
    }

    public override bool SpawnVisitors(Visit visit)
    {
        (GameLocation targetLocation, Point entryPoint) = this.GetSpawnLocation(visit);

        if (entryPoint == Point.Zero)
            return false;

        for (int i = 0; i < visit.group.Count; i++)
        {
            NPC npc = visit.group[i];

            Vector2 warpPosition = new Vector2(entryPoint.X * 64f, entryPoint.Y * 64f);

            Point targetTile = visit.activity.Actors[i].TilePosition;

            targetLocation.addCharacter(npc);
            npc.currentLocation = targetLocation;
            npc.Position = warpPosition;
        }

        return true;
    }

    public override void AfterSpawn(Visit visit)
    {
        // Appear one by one instead of all appearing on one tile at the same time

        for (int i = 0; i < visit.group.Count; i++)
        {
            NPC npc = visit.group[i];
            AccessTools.Field(typeof(Character), "freezeMotion").SetValue(npc, true);
            int delay = i * 800;

            Vector2 pos = npc.Position;
            npc.Position = new Vector2(-1000f, -1000f);
            Game1.delayedActions.Add(new DelayedAction(delay, delegate
            {
                npc.Position = pos;
                AccessTools.Field(typeof(Character), "freezeMotion").SetValue(npc, false);
            }));
        }
    }
}
