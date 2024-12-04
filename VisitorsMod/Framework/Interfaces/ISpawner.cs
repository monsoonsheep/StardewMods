using StardewMods.VisitorsMod.Framework.Data;

namespace StardewMods.VisitorsMod.Framework.Interfaces;
public interface ISpawner
{
    public string Id { get; }

    public int NextArrivalTime { get; }

    public bool IsAvailable();

    public bool StartVisit(Visit visit);

    public bool EndVisit(Visit visit);
}
