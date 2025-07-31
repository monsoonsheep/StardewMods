using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StardewMods.FarmHelpers.Framework.Jobs;
internal class PettingJob : AnimalJob
{
    internal PettingJob(NPC npc, GameLocation location, Action<Job>? onFinish, Point? startingTile) : base(npc, location, onFinish, startingTile)
    {

    }

    protected override bool IsAnimalValid(FarmAnimal animal)
    {
        return animal.wasPet.Value == false && animal.wasAutoPet.Value == false;
    }

    protected override void AnimalAction(FarmAnimal animal)
    {
        Log.Debug("Petting animal");
        animal!.pet(Mod.FakeFarmer, is_auto_pet: false);
    }

    internal static bool IsAvailable(GameLocation location)
    {
        return location.animals.Values.Any(a => !a.wasPet.Value && !a.wasAutoPet.Value);
    }
}
