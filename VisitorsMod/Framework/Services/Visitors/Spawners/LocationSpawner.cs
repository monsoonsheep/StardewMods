using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewMods.SheepCore.Framework.Services;
using StardewMods.VisitorsMod.Framework.Data;
using StardewMods.VisitorsMod.Framework.Interfaces;

namespace StardewMods.VisitorsMod.Framework.Services.Visitors.Spawners;
internal abstract class LocationSpawner : ISpawner
{
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

    public virtual bool SpawnVisitors(Visit visit)
    {
        (GameLocation spawnLocation, Point spawnTile) = this.GetSpawnLocation(visit);
        string targetLocation = visit.activity.Location;

        for (int i = 0; i < visit.group.Count; i++)
        {
            NPC npc = visit.group[i];
            Point targetTile = visit.activity.Actors[i].TilePosition;

            if (npc.currentLocation != null)
                npc.currentLocation.characters.Remove(npc);
            

            spawnLocation.addCharacter(npc);
            npc.currentLocation = spawnLocation;
            npc.Position = spawnTile.ToVector2() * 64f;
        }

        return true;
    }

    public virtual void AfterSpawn(Visit visit)
    {

    }

    public virtual bool EndVisit(Visit visit)
    {
        (GameLocation location, Point tilePoint) = this.GetExitLocation(visit);

        for (int i = 0; i < visit.group.Count; i++)
        {
            NPC npc = visit.group[i];
            AccessTools.Method(typeof(NPC), "prepareToDisembarkOnNewSchedulePath").Invoke(npc, []);

            Point activityPosition = visit.activity.Actors[i].TilePosition;

            Stack<Point>? path = Mod.NpcMovement.PathfindFromLocationToLocation(npc.currentLocation, activityPosition, location, tilePoint, npc);
            if (path == null)
                return false;

            npc.controller = new StardewValley.Pathfinding.PathFindController(path, npc.currentLocation, npc, path.Last())
            {
                NPCSchedule = true
            };
        }

        return true;
    }
}
