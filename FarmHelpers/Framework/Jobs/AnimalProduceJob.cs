using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StardewValley.GameData.FarmAnimals;
using StardewValley.Objects;

namespace StardewMods.FarmHelpers.Framework.Jobs;
internal class AnimalProduceJob : AnimalJob
{
    public AnimalProduceJob(NPC npc, GameLocation location, Action<Job>? onFinish, Point? startingTile) : base(npc, location, onFinish, startingTile)
    {

    }

    internal override void Start(NPC npc)
    {
        Log.Debug("Starting animal produce job");

        base.Start(npc);
    }

    protected override void AnimalAction(FarmAnimal animal)
    {
        StardewValley.Object produce = ItemRegistry.Create<StardewValley.Object>("(O)" + animal.currentProduce.Value);
        produce.CanBeSetDown = false;
        produce.Quality = animal.produceQuality.Value;

        if (animal.hasEatenAnimalCracker.Value)
        {
            produce.Stack = 2;
        }

        Log.Debug($"Harvested {produce.DisplayName} from animal");

        Mod.HelperInventory.Add(produce);
        animal.currentProduce.Value = null;
        animal.ReloadTextureIfNeeded();
    }

    protected override bool IsAnimalValid(FarmAnimal animal)
    {
        return IsAnimalValidButStatic(animal);
    }

    internal static bool IsAnimalValidButStatic(FarmAnimal animal)
    {
        return animal.GetHarvestType() == FarmAnimalHarvestType.HarvestWithTool && animal.currentProduce.Value != null;
    }

    internal static bool IsAvailable(GameLocation location)
    {
        return location.animals.Values
            .Any(IsAnimalValidButStatic) && location.numberOfObjectsWithName("Auto-Grabber") == 0;
    }
}
