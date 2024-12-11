using StardewValley;

namespace StardewMods.FoodJoints.Framework.Interfaces;
public interface IBusSchedulesApi
{
    public int GetMinutesTillNextBus();
    public bool AddVisitorsForNextArrival(NPC npc, int priority = 0);
}
