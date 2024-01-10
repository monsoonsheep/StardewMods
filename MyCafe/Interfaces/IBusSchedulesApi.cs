using StardewValley;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyCafe.Interfaces;
public interface IBusSchedulesApi
{
    public int GetMinutesTillNextBus();
    public bool AddVisitorsForNextArrival(NPC npc, int priority = 0);
}
