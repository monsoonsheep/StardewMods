using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using StardewMods.VisitorsMod.Framework.Data;
using StardewMods.VisitorsMod.Framework.Enums;
using StardewMods.VisitorsMod.Framework.Models.Activities;
using StardewMods.VisitorsMod.Framework.Services.Visitors;
using StardewValley.Pathfinding;

namespace StardewMods.VisitorsMod.Framework.Visitors;
internal abstract class LocationSpawner : ISpawner
{
    protected readonly NpcMovement npcMovement;

    public LocationSpawner(NpcMovement npcMovement)
    {
        this.npcMovement = npcMovement;
    }

    public abstract string Id { get; }

    protected abstract (GameLocation, Point) GetSpawnLocation();

    public virtual int NextArrivalTime
        => Game1.timeOfDay;

    public virtual bool IsAvailable()
        => true;

    public virtual bool StartVisit(Visit visit)
    {
        (GameLocation location, Point tilePoint) = this.GetSpawnLocation();
        string targetLocation = visit.activity.Location;

        for (int i = 0; i < visit.group.Count; i++)
        {
            NPC npc = visit.group[i];

            Point targetTile = visit.activity.Actors[i].TilePosition;

            location.addCharacter(npc);
            npc.currentLocation = location;
            npc.Position = tilePoint.ToVector2() * 64f;

            if (!this.npcMovement.NpcPathTo(npc, Game1.getLocationFromName(targetLocation), targetTile))
                return false;
        }

        return true;
    }

    public bool EndVisit(Visit visit)
    {
        (GameLocation location, Point tilePoint) = this.GetSpawnLocation();

        for (int i = 0; i < visit.group.Count; i++)
        {
            NPC npc = visit.group[i];

            if (!this.npcMovement.NpcPathTo(npc, location, tilePoint))
                return false;
        }

        return true;
    }
}
