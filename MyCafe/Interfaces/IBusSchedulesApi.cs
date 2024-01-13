using StardewValley;

namespace MyCafe.Interfaces;
public interface IBusSchedulesApi
{
    public int GetMinutesTillNextBus();
    public bool AddVisitorsForNextArrival(NPC npc, int priority = 0);
}
