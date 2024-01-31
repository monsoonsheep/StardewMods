using StardewValley;

namespace BusSchedules.Interfaces;

public interface IApi
{
    public int GetMinutesTillNextBus();
    public bool AddVisitorsForNextArrival(NPC npc, int priority = 0);
}

public class Api : IApi
{
    public int GetMinutesTillNextBus()
    {
        return (Mod.Instance.BusEnabled) ? Mod.Instance.TimeUntilNextArrival : 99999;
    }

    public bool AddVisitorsForNextArrival(NPC npc, int priority = 0)
    {
        Mod.Instance.VisitorsForNextArrival.Enqueue(npc, priority);
        return true;
    }
}