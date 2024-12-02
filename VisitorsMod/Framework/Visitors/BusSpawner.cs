using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewMods.VisitorsMod.Framework.Data;
using StardewMods.VisitorsMod.Framework.Interfaces;
using StardewMods.VisitorsMod.Framework.Models.Activities;
using StardewMods.VisitorsMod.Framework.Services.Visitors;

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
    
    protected override (GameLocation, Point) GetSpawnLocation()
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
