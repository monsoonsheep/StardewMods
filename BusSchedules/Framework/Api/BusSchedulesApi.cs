using StardewMods.BusSchedules.Framework.Services;

namespace StardewMods.BusSchedules.Framework.Api;
public class BusSchedulesApi : IBusSchedulesApi
{
    private readonly NpcArrivals visitorSpawning;
    private readonly BusManager busManager;

    internal BusSchedulesApi(NpcArrivals visitorSpawning, BusManager busManager)
    {
        this.visitorSpawning = visitorSpawning;
        this.busManager = busManager;
    }

    public bool IsAvailable()
        => this.busManager.BusEnabled;

    public int NextArrivalTime
        => this.busManager.Timings.NextArrivalTime;

    public Point BusTilePosition
        => Values.BusDoorTile;

    public void AddVisitor(NPC npc)
    {
        this.visitorSpawning.AddVisitor(npc);
    }
}
