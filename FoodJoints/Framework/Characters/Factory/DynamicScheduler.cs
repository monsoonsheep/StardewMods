using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StardewMods.FoodJoints.Framework.Enums;

namespace StardewMods.FoodJoints.Framework.Characters.Factory;
internal class DynamicScheduler
{

    internal int TrySpawn()
    {
        int count = 0;

        // Choose between villager spawn and non-villager spawn
        float weight = Mod.Config.EnableRandomlyGeneratedCustomers / 5f;

        float prob = this.GetChanceToSpawnRandom(weight);

        Log.Trace($"(Chance to spawn: {prob})");

        // Try chance
        float random;

        while ((random = Game1.random.NextSingle()) <= prob)
        {
            count++;
            prob -= 0.3f;
        }

        return count;
    }

    private float GetChanceToSpawnRandom(float baseProb)
    {
        int totalTimeIntervalsDuringDay = (Mod.Cafe.ClosingTime - Mod.Cafe.OpeningTime) / 10;
        int minutesTillCloses = StardewValley.Utility.CalculateMinutesBetweenTimes(Game1.timeOfDay, Mod.Cafe.ClosingTime);
        int minutesSinceLastVisitors = StardewValley.Utility.CalculateMinutesBetweenTimes(Mod.Cafe.LastTimeCustomersArrived, Game1.timeOfDay);
        if (minutesTillCloses <= 20 || totalTimeIntervalsDuringDay == 0)
            return -10.00f;

        // base
        float prob = 1f / (float)Math.Pow(totalTimeIntervalsDuringDay, 0.5f);

        // more chance if it's been a while since last Visitors
        // TODO maybe mult this at the end
        prob += minutesSinceLastVisitors switch
        {
            <= 30 => -0.1f,
            <= 60 => Game1.random.Next(5) == 0 ? 0.05f : -0.10f,
            <= 100 => Game1.random.Next(2) == 0 ? -0.08f : 0.08f,
            <= 120 => 0.2f,
            >= 130 => 0.4f,
            _ => 0.05f
        };

        // slight chance to spawn if last hour of open time
        if (minutesTillCloses <= 60)
            prob += Game1.random.Next(20 + Math.Max(0, minutesTillCloses / 3)) >= 28
                ? 0.10f
                : -0.20f;

        prob *= ((float)Math.Pow(baseProb, 0.5) * 1.50f);

#if DEBUG
        //prob += Debug.ExtraProbabilityToSpawn;
#endif

        return Math.Max(prob, 1f);
    }
}
