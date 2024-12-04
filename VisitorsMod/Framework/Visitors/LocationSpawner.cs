using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewMods.SheepCore.Framework.Services;
using StardewMods.VisitorsMod.Framework.Data;

namespace StardewMods.VisitorsMod.Framework.Visitors;
internal abstract class LocationSpawner : ISpawner
{
    protected readonly NpcMovement npcMovement;

    public LocationSpawner(NpcMovement npcMovement)
    {
        this.npcMovement = npcMovement;
    }

    public abstract string Id { get; }

    protected abstract (GameLocation, Point) GetSpawnLocation(Visit visit);

    protected virtual (GameLocation, Point) GetExitLocation(Visit visit)
    {
        return this.GetSpawnLocation(visit);
    }

    public virtual int NextArrivalTime
        => Game1.timeOfDay;

    public virtual bool IsAvailable()
        => true;

    public virtual bool StartVisit(Visit visit)
    {
        (GameLocation location, Point tilePoint) = this.GetSpawnLocation(visit);
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

    public virtual bool EndVisit(Visit visit)
    {
        (GameLocation location, Point tilePoint) = this.GetExitLocation(visit);

        for (int i = 0; i < visit.group.Count; i++)
        {
            NPC npc = visit.group[i];
            AccessTools.Method(typeof(NPC), "prepareToDisembarkOnNewSchedulePath").Invoke(npc, []);

            Point activityPosition = visit.activity.Actors[i].TilePosition;
            if (!this.npcMovement.NpcPathToFrom(npc, npc.currentLocation, activityPosition, location, tilePoint))
                return false;
        }

        return true;
    }
}