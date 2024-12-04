using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewMods.VisitorsMod.Framework.Data;
using StardewMods.SheepCore.Framework.Services;
using StardewMods.VisitorsMod.Framework.Interfaces;

namespace StardewMods.VisitorsMod.Framework.Services.Visitors.Spawners;
internal class WarpSpawner : LocationSpawner, ISpawner
{
    public override string Id
        => "Warp";

    protected override (GameLocation, Point) GetSpawnLocation(Visit visit)
    {
        GameLocation location = Game1.getLocationFromName(visit.activity.Location);
        Point entryPoint = Point.Zero;

        Warp warpOut = location.GetFirstPlayerWarp();
        GameLocation warpedLocation = Game1.getLocationFromName(warpOut.TargetName);
        Point warpedOutTile = new Point(warpOut.TargetX, warpOut.TargetY);

        foreach (Warp w in warpedLocation.warps)
        {
            if (Math.Abs(w.X - warpedOutTile.X) <= 2 && Math.Abs(w.Y - warpedOutTile.Y) <= 2)
            {
                entryPoint = new Point(w.TargetX, w.TargetY);
                break;
            }
        }

        foreach (KeyValuePair<Point, string> door in warpedLocation.doors.Pairs)
        {
            if ((door.Value == location.Name || door.Value == location.NameOrUniqueName)
                && Math.Abs(door.Key.X - warpedOutTile.X) <= 2 && Math.Abs(door.Key.Y - warpedOutTile.Y) <= 2)
            {
                entryPoint = warpedLocation.getWarpPointTarget(door.Key);
                break;
            }
        }

        return (location, entryPoint);
    }

    public override bool StartVisit(Visit visit)
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

            if (!ModEntry.NpcMovement.NpcPathTo(npc, targetLocation, targetTile))
            {
                return false;
            }

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
        return true;
    }
}
