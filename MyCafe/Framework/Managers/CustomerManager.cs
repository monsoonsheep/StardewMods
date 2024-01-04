using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MyCafe.Framework.Customers;
using MyCafe.Framework.Objects;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Pathfinding;
using SUtility = StardewValley.Utility;

namespace MyCafe.Framework.Managers;

internal sealed class CustomerManager
{
    internal static CustomerManager Instance;

    internal readonly Dictionary<string, CustomerData> CustomersData = new();
    internal readonly List<CustomerGroup> CurrentGroups = new();
    internal readonly Dictionary<string, ScheduleData> VillagerCustomerSchedules = new();

    internal IEnumerable<Customer> CurrentCustomers 
        => CurrentGroups.SelectMany(g => g.Members);

    internal CustomerManager() => Instance = this;

    internal List<CustomerData> GetCustomers(int maxMembers) {
        return new List<CustomerData>() {CustomersData.First().Value};
    }

    internal void RemoveAllCustomers() {

    }

    internal void PopulateCustomersData()
    {
        foreach (CustomerModel model in AssetManager.CustomerModels)
        {
            CustomersData[model.Name] = new CustomerData()
            {
                Model = model
            };
        }
    }

    internal bool CanNpcVisitDuringTime(NPC npc, int timeOfDay)
    {
        ScheduleData visitData = VillagerCustomerSchedules[npc.Name];

        if (CurrentCustomers.Contains(npc) ||
            npc.isSleeping.Value || 
            npc.ScheduleKey == null || 
            visitData.CanVisitToday == false ||
            visitData.LastVisitedDate == Game1.Date) 
            return false;


        visitData.BusyTimes.TryGetValue(npc.ScheduleKey, out var busyPeriods);
        if (busyPeriods == null)
            return true; // If no busy period for today, they're free all day

        // Check their busy periods for their current schedule key
        foreach (BusyPeriod busyPeriod in busyPeriods)
        {
            if (SUtility.CalculateMinutesBetweenTimes(timeOfDay, busyPeriod.From) <= 120
                && SUtility.CalculateMinutesBetweenTimes(timeOfDay, busyPeriod.To) > 0)
            {
                if (!(busyPeriod.Priority <= 3 && Game1.random.Next(6 * busyPeriod.Priority) == 0) &&
                    !(busyPeriod.Priority == 4 && Game1.random.Next(50) == 0))
                {
                    return false;
                }
            }
        }

        return true;
    }

    internal void UpdateNpcSchedules()
    {
        // Set which NPCs can visit today based on how many days it's been since their last visit, and their 
        // visit frequency level given in their visit data.
        foreach (var data in VillagerCustomerSchedules)
        {
            int daysSinceLastVisit = Game1.Date.TotalDays - data.Value.LastVisitedDate.TotalDays;
            int daysAllowedBetweenVisits = data.Value.Frequency switch
            {
                0 => 200,
                1 => 28,
                2 => 15,
                3 => 8,
                4 => 2,
                5 => 0,
                _ => 9999999
            };

            data.Value.CanVisitToday = daysSinceLastVisit > daysAllowedBetweenVisits;
        }
    }
}
