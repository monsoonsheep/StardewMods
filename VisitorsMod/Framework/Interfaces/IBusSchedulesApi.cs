using Microsoft.Xna.Framework;

namespace StardewMods.VisitorsMod.Framework.Interfaces;
public interface IBusSchedulesApi
{
    public Point BusTilePosition { get; }

    public bool IsAvailable();

    public int NextArrivalTime { get; }

    public void AddVisitor(NPC npc);
}
