using StardewMods.VisitorsMod.Framework.Data;

namespace StardewMods.VisitorsMod.Framework.Interfaces;
public interface ISpawner
{
    public string Id { get; }

    public int NextArrivalTime { get; }

    public bool IsAvailable();

    public bool SpawnVisitors(Visit visit);

    public void AfterSpawn(Visit visit);

    public bool EndVisit(Visit visit);
}
