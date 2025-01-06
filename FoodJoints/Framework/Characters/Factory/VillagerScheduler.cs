using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StardewMods.FoodJoints.Framework.Data;

namespace StardewMods.FoodJoints.Framework.Characters.Factory;
internal class VillagerScheduler
{
    private Dictionary<int, VillagerCustomerData> villagerCustomerSchedule = new();

    internal void ScheduleArrivals()
    {
        int totalTimeIntervalsDuringDay = (Mod.Cafe.ClosingTime - Mod.Cafe.OpeningTime) / 10;
        var intervals = Enumerable.Range(0, totalTimeIntervalsDuringDay).Select(i => Utility.ModifyTime(Mod.Cafe.OpeningTime, (i * 10)));

        foreach (VillagerCustomerData data in Mod.Customers.VillagerData.Values)
        {
            List<(int, int)>? freePeriods = data.FreePeriods;

            int time = 0;

            this.villagerCustomerSchedule[time] = data;
        }
    }

}
