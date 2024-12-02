using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StardewMods.BusSchedules.Framework.Services;

namespace StardewMods.BusSchedules.Framework.Api;
internal class BusSchedulesApi : IBusSchedulesApi
{
    private readonly NpcArrivals visitorSpawning;
    private readonly Timings timings;
    private readonly BusManager busManager;

    internal BusSchedulesApi(NpcArrivals visitorSpawning, Timings timings, BusManager busManager)
    {
        this.visitorSpawning = visitorSpawning;
        this.timings = timings;
        this.busManager = busManager;
    }

    public bool IsAvailable()
        => this.busManager.BusEnabled;

    public int NextArrivalTime
        => this.timings.NextArrivalTime;

    public Point BusTilePosition
        => Values.BusDoorTile;

    public void AddVisitor(NPC npc)
    {
        this.visitorSpawning.AddVisitor(npc);
    }
}
