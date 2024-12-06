using StardewValley;

namespace StardewMods.MyShops.Framework.Interfaces;
public interface IBusSchedulesApi
{
    public int GetMinutesTillNextBus();
    public bool AddVisitorsForNextArrival(NPC npc, int priority = 0);
}
