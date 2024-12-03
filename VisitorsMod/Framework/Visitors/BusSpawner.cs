using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewMods.SheepCore.Framework.Services;
using StardewMods.VisitorsMod.Framework.Data;
using StardewMods.VisitorsMod.Framework.Interfaces;

namespace StardewMods.VisitorsMod.Framework.Visitors;
internal class BusSpawner : LocationSpawner, ISpawner
{
    private readonly IBusSchedulesApi api;

    public BusSpawner(IBusSchedulesApi api, NpcMovement npcMovement) : base(npcMovement)
    {
        this.api = api;
    }

    public override string Id
        => "Bus";

    public override int NextArrivalTime
        => this.api.NextArrivalTime;

    public override bool IsAvailable()
        => this.api.IsAvailable();
    
    protected override (GameLocation, Point) GetSpawnLocation(Visit visit)
    {
        return (Game1.getLocationFromName("BusStop"), this.api.BusTilePosition);
    }

    public override bool StartVisit(Visit visit)
    {
        if (base.StartVisit(visit))
        {
            foreach (NPC npc in visit.group)
            {
                npc.Position = new Vector2(-1000f, -1000f);
                AccessTools.Field(typeof(Character), "freezeMotion").SetValue(npc, true);
                this.api.AddVisitor(npc);
            }
            return true;
        }

        return false;
    }
}
