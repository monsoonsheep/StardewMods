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

    internal void SpawnCustomerOnRoad()
    {
        string name = CustomersData.Keys.FirstOrDefault();
        if (name == null || !CustomersData.TryGetValue(name, out CustomerData data) || CafeManager.Instance.CafeIndoors == null)
            return;

        Texture2D portrait = Game1.content.Load<Texture2D>(data.Model.PortraitName);
        Customer c = new Customer(
            $"CustomerNPC_{name}", 
            new Vector2(10, 12) * 64f, 
            "BusStop", 
            new AnimatedSprite(data.Model.TilesheetPath, 0, 16, 32),
            portrait
            );
        

        Table table = TableManager.Instance.CurrentTables.Where(t => !t.IsReserved).MinBy(_ => Game1.random.Next());
        if (table == null) {
            Log.Debug("No tables available");
            return;
        }

        table.Reserve(new() {c});
        c.ReservedSeat.Reserve(c);

        GameLocation tableLocation = Utility.GetLocationFromName(table.CurrentLocation);
        if (tableLocation == null) {
            Log.Debug("Couldn't find location for table to reserve");
            return;
        }

        GameLocation busStop = Game1.getLocationFromName("BusStop");
        busStop.addCharacter(c);
        c.PathTo(tableLocation, c.ReservedSeat.Position.ToPoint(), 3, null);
        if (c.controller == null || c.controller.pathToEndPoint?.Count == 0) {
            busStop.characters.Remove(c);
            return;
        }

        if (!c.controller.pathToEndPoint.Last().Equals(c.ReservedSeat.Position.ToPoint())) {
            int direction = Utility.DirectionIntFromVectors(c.controller.pathToEndPoint.Last().ToVector2(), c.ReservedSeat.Position);
            c.controller.endBehaviorFunction = (_, _) => 
            { 
                c.SitDown(direction);
                c.faceDirection(c.ReservedSeat.SittingDirection);
            };
        }
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

    internal float GetChanceToSpawnCustomer(int openingTime, int closingTime, int lastTimeCustomersArrived, int numberOfFreeTables, int totalNumberOfTables)
    {
        int minutesTillCloses = SUtility.CalculateMinutesBetweenTimes(Game1.timeOfDay, closingTime);
        int minutesTillOpens = SUtility.CalculateMinutesBetweenTimes(Game1.timeOfDay, openingTime);
        int minutesSinceLastVisitors = SUtility.CalculateMinutesBetweenTimes(lastTimeCustomersArrived, Game1.timeOfDay);
        float percentageOfTablesFree = (float) numberOfFreeTables / totalNumberOfTables;

        float prob = 0f;

        // more chance if it's been a while since last Visitors
        prob += minutesSinceLastVisitors switch
        {
            <= 20 => 0f,
            <= 30 => Game1.random.Next(5) == 0 ? 0.05f : -0.1f,
            <= 60 => Game1.random.Next(2) == 0 ? 0.1f : 0f,
            _ => 0.25f
        };

        // more chance if a higher percent of tables are free
        prob += (percentageOfTablesFree) switch
        {
            <= 0.2f => 0.0f,
            <= 0.5f => 0.1f,
            <= 0.8f => 0.15f,
            _ => 0.2f
        };

        // slight chance to spawn if last hour of open time
        if (minutesTillCloses <= 60)
            prob += (Game1.random.Next(20 + (minutesTillCloses / 3)) >= 28) ? 0.2f : -0.5f;

        return prob;
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
