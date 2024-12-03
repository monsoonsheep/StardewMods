namespace StardewMods.BusSchedules.Framework.Api;
public interface IBusSchedulesApi
{
    public Point BusTilePosition { get; }

    public int NextArrivalTime { get; }

    public bool IsAvailable();

    public void AddVisitor(NPC npc);
}
