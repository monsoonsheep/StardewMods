using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StardewMods.FarmHelpers.Framework;
internal abstract class AnimalJob : Job
{
    private List<FarmAnimal> remainingAnimals = [];
    private FarmAnimal? currentAnimal;

    internal AnimalJob(NPC npc, GameLocation location, Action<Job>? onFinish, Point? startingTile) : base(npc, location, onFinish)
    {
        base.startPoint = startingTile;
    }

    internal override void Start(NPC npc)
    {
        foreach (FarmAnimal animal in this.location.animals.Values)
        {
            if (this.IsAnimalValid(animal))
            {
                this.remainingAnimals.Add(animal);
            }
        }

        this.NextAnimal(npc);
    }

    private void NextAnimal(NPC npc)
    {
        if (this.remainingAnimals.Count == 0)
        {
            base.Finish(npc);
            return;
        }

        // Select closest animal
        this.currentAnimal = this.remainingAnimals.MinBy((animal) => Utility.distance(
                    npc.TilePoint.X,
                    animal.TilePoint.X,
                    npc.TilePoint.Y,
                    animal.TilePoint.Y))!;

        this.remainingAnimals.Remove(this.currentAnimal);

        // Find position to stand for petting, and select closest
        Point? standingTile = ModUtility.GetEmptyTilesNextTo(this.location, this.currentAnimal.TilePoint)
            .MinBy(p => Utility.distance(npc.TilePoint.X, p.X, npc.TilePoint.Y, p.Y));

        if (!standingTile.HasValue)
        {
            this.NextAnimal(npc);
            return;
        }

        // Move to pet
        this.currentAnimal.pauseTimer = 9999;
        HelperManager.MoveHelper(this.location, standingTile.Value, this.OnReachAnimal);
    }

    private void OnReachAnimal(NPC npc)
    {
        this.AnimalAction(this.currentAnimal!);

        int pause = Game1.random.Next(300, 1500);
        this.currentAnimal!.pauseTimer = pause;
        ModUtility.AddDelayedAction(() => this.NextAnimal(npc), pause);
    }

    protected abstract bool IsAnimalValid(FarmAnimal animal);

    protected abstract void AnimalAction(FarmAnimal animal);
}
