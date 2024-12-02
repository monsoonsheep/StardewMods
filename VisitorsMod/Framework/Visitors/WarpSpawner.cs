using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewMods.VisitorsMod.Framework.Data;
using StardewMods.VisitorsMod.Framework.Enums;
using StardewMods.VisitorsMod.Framework.Models.Activities;
using StardewMods.VisitorsMod.Framework.Services.Visitors;

namespace StardewMods.VisitorsMod.Framework.Visitors;
internal class WarpSpawner : LocationSpawner, ISpawner
{
    public WarpSpawner(NpcMovement npcMovement) : base(npcMovement)
    {

    }

    public override string Id
        => "Warp";

    protected override (GameLocation, Point) GetSpawnLocation()
        => throw new NotImplementedException();
    
    public override bool StartVisit(Visit visit)
    {
        GameLocation targetLocation = Game1.getLocationFromName(visit.activity.Location);
        
        Point? entryPoint = this.GetEntryWarpPoint(targetLocation);
        if (entryPoint == null)
            return false;

        for (int i = 0; i < visit.group.Count; i++)
        {
            NPC npc = visit.group[i];
            int delay = i * 800;

            Vector2 warpPosition = new Vector2(entryPoint.Value.X * 64f, entryPoint.Value.Y * 64f);

            Point targetTile = visit.activity.Actors[i].TilePosition;

            targetLocation.addCharacter(npc);
            npc.currentLocation = targetLocation;
            npc.Position = warpPosition;

            if (!this.npcMovement.NpcPathTo(npc, targetLocation, targetTile))
            {
                return false;
            }
            Vector2 pos = npc.Position;

            npc.Position = new Vector2(-1000f, -1000f);
            AccessTools.Field(typeof(Character), "freezeMotion").SetValue(npc, true);
            Game1.delayedActions.Add(new DelayedAction(delay, delegate
            {
                npc.Position = pos;
                AccessTools.Field(typeof(Character), "freezeMotion").SetValue(npc, false);
            }));
        }
        return true;
    }

    private Point? GetEntryWarpPoint(GameLocation location)
    {
        Warp entryWarp = location.GetFirstPlayerWarp();
        GameLocation warpedLocation = Game1.getLocationFromName(entryWarp.TargetName);
        Point warpedTile = new Point(entryWarp.TargetX, entryWarp.TargetY);

        foreach (Warp w in warpedLocation.warps)
        {
            if (Math.Abs(w.X - warpedTile.X) <= 2 &&  Math.Abs(w.Y - warpedTile.Y) <= 2)
                return new Point(w.TargetX, w.TargetY);
        }

        return null;
    }
}
