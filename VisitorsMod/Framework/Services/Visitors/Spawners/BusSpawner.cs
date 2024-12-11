using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewMods.SheepCore.Framework.Services;
using StardewMods.VisitorsMod.Framework.Data;
using StardewMods.VisitorsMod.Framework.Interfaces;

namespace StardewMods.VisitorsMod.Framework.Services.Visitors.Spawners;
internal class BusSpawner : LocationSpawner, ISpawner
{
    private readonly IBusSchedulesApi busSchedulesApi;

    public BusSpawner(IBusSchedulesApi api) : base()
    {
        this.busSchedulesApi = api;
    }

    public override string Id
        => "Bus";

    public override int NextArrivalTime
        => this.busSchedulesApi.NextArrivalTime;

    public override bool IsAvailable()
        => this.busSchedulesApi.IsAvailable();

    protected override (GameLocation, Point) GetSpawnLocation(Visit visit)
    {
        return (Game1.getLocationFromName("BusStop"), this.busSchedulesApi.BusTilePosition);
    }

    public override void AfterSpawn(Visit visit)
    {
        foreach (NPC npc in visit.group)
        {
            npc.Position = new Vector2(-1000f, -1000f);
            AccessTools.Field(typeof(Character), "freezeMotion").SetValue(npc, true);
            this.busSchedulesApi.AddVisitor(npc);
        }
    }
}
