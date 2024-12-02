using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StardewMods.BusSchedules.Framework.Api;
public interface IBusSchedulesApi
{
    public Point BusTilePosition { get; }

    public int NextArrivalTime { get; }

    public bool IsAvailable();

    public void AddVisitor(NPC npc);
}
