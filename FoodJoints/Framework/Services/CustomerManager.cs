using System.Text.RegularExpressions;
using StardewMods.ExtraNpcBehaviors.Framework.Data;
using StardewMods.FoodJoints.Framework.Characters;
using StardewMods.FoodJoints.Framework.Characters.Factory;
using StardewMods.FoodJoints.Framework.Data;
using StardewMods.FoodJoints.Framework.Data.Models;
using StardewMods.FoodJoints.Framework.Enums;
using StardewMods.FoodJoints.Framework.Game;
using StardewMods.FoodJoints.Framework.Objects;
using StardewMods.SheepCore.Framework.Services;
using StardewValley.Pathfinding;
using StardewValley.SpecialOrders.Objectives;

namespace StardewMods.FoodJoints.Framework.Services;
internal class CustomerManager
{
    internal static CustomerManager Instance = null!;

    internal Dictionary<string, CustomerData> CustomerData = [];
    internal Dictionary<string, VillagerCustomerModel> VillagerCustomerModels = [];
    internal Dictionary<string, VillagerCustomerData> VillagerData = [];

    private VillagerCustomerBuilder villagerCustomerBuilder = null!;
    internal readonly List<CustomerGroup> Groups = [];

    private Dictionary<int, VillagerCustomerData> villagerCustomerSchedule = new();

    internal CustomerManager()
        => Instance = this;

    internal void Initialize()
    {
        this.villagerCustomerBuilder = new VillagerCustomerBuilder();

        Mod.Events.GameLoop.SaveLoaded += this.OnSaveLoaded;
        Mod.Events.GameLoop.DayStarted += this.OnDayStarted;
        Mod.Events.GameLoop.DayEnding += this.OnDayEnding;
    }

    private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
    {
        foreach (var model in this.VillagerCustomerModels)
            if (!this.VillagerData.ContainsKey(model.Key))
                this.VillagerData[model.Key] = new VillagerCustomerData(model.Key);

        this.CleanUpCustomers();
    }

    private void OnDayStarted(object? sender, DayStartedEventArgs e)
    {

    }

    private void OnDayEnding(object? sender, DayEndingEventArgs e)
    {
        if (!Context.IsMainPlayer)
            return;

        // Delete customers
        this.RemoveAllCustomers();
        this.CleanUpCustomers();
    }

    internal void ScheduleArrivals()
    {
        int totalTimeIntervalsDuringDay = (Mod.Cafe.ClosingTime - Mod.Cafe.OpeningTime) / 10;
        var intervals = Enumerable.Range(0, totalTimeIntervalsDuringDay).Select(i => Utility.ModifyTime(Mod.Cafe.OpeningTime, (i * 10)));

        foreach (VillagerCustomerData data in this.VillagerData.Values)
        {
            List<(int, int)>? freePeriods = data.FreePeriods;

            int time = 0;

            this.villagerCustomerSchedule[time] = data;
        }
    }

    private bool CanVillagerVisit(VillagerCustomerData data)
    {
        NPC npc = data.GetNpc();
        VillagerCustomerModel model = Mod.Customers.VillagerCustomerModels[data.NpcName];

        int daysAllowed = model.VisitFrequency switch
        {
            1 => 27, 2 => 13, 3 => 7, 4 => 3, 5 => 1, _ => 9999999
        };
        int daysSinceLastVisit = Game1.Date.TotalDays - data.LastVisitedDate.TotalDays;

#if DEBUG
        daysAllowed = 1;
#endif

        if (npc == null ||
            npc.ScheduleKey == null ||
            npc.controller != null ||
            daysSinceLastVisit < daysAllowed)
            return false;

        return true;
    }

    internal void CustomerSpawningUpdate()
    {
        

        // Choose between villager spawn and non-villager spawn
        float weightForVillagers = Mod.Config.EnableNpcCustomers / 5f;
        float weightForRandom = Math.Max(Mod.Config.EnableCustomCustomers, Mod.Config.EnableRandomlyGeneratedCustomers) / 5f;

        float prob = this.ProbabilityToSpawnCustomers(Math.Max(weightForVillagers, weightForRandom));

        Log.Trace($"(Chance to spawn: {prob})");

        #if DEBUG
        prob += 0.4f;
        #endif

        // Try chance
        float random;

        while ((random = Game1.random.NextSingle()) <= prob)
        {
            // REMOVE
            weightForVillagers = 1f;
            weightForRandom = 0f;

            if ((random * (weightForRandom + weightForVillagers)) < weightForVillagers)
                this.SpawnVillagerCustomers();

            prob -= 0.25f;
        }
    }

    private float ProbabilityToSpawnCustomers(float baseProb)
    {
        int totalTimeIntervalsDuringDay = (Mod.Cafe.ClosingTime - Mod.Cafe.OpeningTime) / 10;
        int minutesTillCloses = StardewValley.Utility.CalculateMinutesBetweenTimes(Game1.timeOfDay, Mod.Cafe.ClosingTime);
        int minutesSinceLastVisitors = StardewValley.Utility.CalculateMinutesBetweenTimes(Mod.Cafe.LastTimeCustomersArrived, Game1.timeOfDay);
        if (minutesTillCloses <= 20 || totalTimeIntervalsDuringDay == 0)
            return -10.00f;

        // base
        float prob = 1f / (float) Math.Pow(totalTimeIntervalsDuringDay, 0.5f);

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

        prob *= ((float) Math.Pow(baseProb, 0.5) * 1.50f);

#if DEBUG
        //prob += Debug.ExtraProbabilityToSpawn;
#endif

        return Math.Max(prob, 1f);
    }

    internal void SpawnVillagerCustomers()
    {
        Table? table = Mod.Tables.GetFreeTable();
        if (table != null)
        {
            CustomerGroup? group = this.villagerCustomerBuilder.TrySpawn(table);
            if (group != null)
                this.AddGroup(group);
        }
    }

    internal void AddGroup(CustomerGroup group)
    {
        this.Groups.Add(group);
        Mod.Cafe.LastTimeCustomersArrived = Game1.timeOfDay;
    }

    internal Item? GetMenuItemForCustomer(NPC npc)
    {
        SortedDictionary<int, List<Item>> tastesItems = [];
        int count = 0;

        foreach (Item i in Mod.Cafe.Menu.ItemDictionary.Values.SelectMany(i => i))
        {
            int tasteLevel;
            switch (npc.getGiftTasteForThisItem(i))
            {
                case 6:
                    tasteLevel = (int)GiftObjective.LikeLevels.Hated;
                    break;
                case 4:
                    tasteLevel = (int)GiftObjective.LikeLevels.Disliked;
                    break;
                case 8:
                    tasteLevel = (int)GiftObjective.LikeLevels.Neutral;
                    break;
                case 2:
                    tasteLevel = (int)GiftObjective.LikeLevels.Liked;
                    break;
                case 0:
                    tasteLevel = (int)GiftObjective.LikeLevels.Loved;
                    break;
                default:
                    continue;
            }

            if (!tastesItems.ContainsKey(tasteLevel))
                tastesItems[tasteLevel] = [];

            tastesItems[tasteLevel].Add(i);
            count++;
        }

        // Null if either no items on menu or the best the menu can do for the npc is less than neutral taste for them
        if (count == 0 || tastesItems.Keys.Max() <= (int)GiftObjective.LikeLevels.Disliked)
            return null;

        return tastesItems[tastesItems.Keys.Max()].PickRandom();
    }

    internal void EndCustomerGroup(CustomerGroup group)
    {
        Log.Debug($"Removing customers");

        group.ReservedTable!.Free();
        this.Groups.Remove(group);

        GameLocation tableLocation = Game1.getLocationFromName(group.ReservedTable!.Location);
        GameLocation busStop = Game1.getLocationFromName("BusStop");

        bool currentlyInFarmLocation = (tableLocation is Farm || tableLocation.ParentBuilding?.parentLocationName.Value.Equals("Farm") is true);

        foreach (NPC n in group.Members)
        {
            // ExtraNpcBehaviors will make them get up from chair
            AccessTools.Method(typeof(NPC), "finishEndOfRouteAnimation", [])?.Invoke(n, []);

            // End of get-up lerp from ExtraNpcBehaviors
            Vector2 v = n.get_lerpEndPosition();
            Point getUpPosition = (v / 64f).ToPoint();

            if (group.Type != GroupType.Villager)
            {
                if (Mod.Customers.CustomerData.TryGetValue(n.Name, out CustomerData? data))
                    data.LastVisitedData = Game1.Date;

                bool res = n.MoveTo(tableLocation, getUpPosition, busStop, new Point(33, 9), (c, loc) => this.DeleteNpcFromExistence((c as NPC)!));
                if (!res)
                    this.DeleteNpcFromExistence(n);
            }
            else
            {
                Mod.Customers.VillagerData[n.Name].LastAteFood = n.get_OrderItem().Value!.QualifiedItemId;
                Mod.Customers.VillagerData[n.Name].LastVisitedDate = Game1.Date;
                n.faceTowardFarmerTimer = 0;
                n.faceTowardFarmer = false;
                n.movementPause = 0;

                if (currentlyInFarmLocation)
                {
                    // if failed, warp home TODO
                    bool res = n.MoveTo(tableLocation, getUpPosition, busStop, new Point(14, 24), (c, _) => this.ReturnVillagerToSchedule((c as NPC)!));
                }
                else
                {
                    this.ReturnVillagerToSchedule(n);
                }
            }
        }
    }

    internal void ReturnVillagerToSchedule(NPC npc)
    {
        npc.ignoreScheduleToday = false;
        npc.EventActor = false;
        npc.get_OrderItem().Set(null);
        npc.set_Seat(null);
        this.ResetNpc(npc);

        int timeOfActivity = this.GetOriginalScheduleTime(npc);
        SchedulePathDescription originalPathDescription = npc.Schedule[timeOfActivity];

        Log.Trace($"[{Game1.timeOfDay}] Returning {npc.Name} to schedule for key {npc.ScheduleKey}.");
        Log.Trace($"Choosing {timeOfActivity}.");
        Log.Trace($"Schedule description is {originalPathDescription.targetLocationName}: {originalPathDescription.targetTile}, behavior: {originalPathDescription.endOfRouteBehavior}");

        SchedulePathDescription? sched = null;
        try
        {
            sched = npc.pathfindToNextScheduleLocation(npc.ScheduleKey, npc.currentLocation.Name, npc.TilePoint.X, npc.TilePoint.Y,
                originalPathDescription.targetLocationName,
                originalPathDescription.targetTile.X, originalPathDescription.targetTile.Y, originalPathDescription.facingDirection,
                originalPathDescription.endOfRouteBehavior,
                originalPathDescription.endOfRouteMessage);
        }
        catch (Exception e)
        {
            Log.Error($"{e.Message}\n{e.StackTrace}");
        }

        if (sched == null)
        {
            Log.Error("Couldn't return NPC to schedule");
            // TODO warp them
            return;
        }

        npc.Schedule.Remove(timeOfActivity);
        npc.lastAttemptedSchedule = Game1.timeOfDay - 10;
        npc.queuedSchedulePaths.Clear();
        npc.Schedule[Game1.timeOfDay] = sched;
        npc.checkSchedule(Game1.timeOfDay);

        if (npc.controller == null)
        {
            Log.Error("checkSchedule didn't set the controller");
            // TODO warp them
        }
    }

    internal void DeleteNpcFromExistence(NPC npc)
    {
        npc.currentLocation?.characters.Remove(npc);
        this.ResetNpc(npc);
        Match findRandomGuid = new Regex($@"{Values.CUSTOMER_NPC_NAME_PREFIX}Random(.*)").Match(npc.Name);
        if (findRandomGuid.Success)
        {
            Log.Trace("Deleting random customer and its generated sprite");
            string guid = findRandomGuid.Groups[1].Value;
            //if (this.GeneratedSprites.Remove(guid) == false)
            //    Log.Trace("Tried to remove GUID for random customer but it wasn't registered.");
        }
    }

    internal void ResetNpc(NPC npc)
    {

    }

    internal void CleanUpCustomers()
    {
        Utility.ForEachLocation((loc) =>
        {
            for (int i = loc.characters.Count - 1; i >= 0; i--)
            {
                NPC npc = loc.characters[i];
                if (npc.Name.StartsWith(Values.CUSTOMER_NPC_NAME_PREFIX))
                {
                    loc.characters.RemoveAt(i);
                }
            }

            return true;
        });
    }

    internal void RemoveAllCustomers()
    {
        for (int i = this.Groups.Count - 1; i >= 0; i--)
        {
            var g = this.Groups[i];
            this.EndCustomerGroup(g);
        }
    }

    internal int GetOriginalScheduleTime(NPC npc)
    {
        List<int> activityTimes = npc.Schedule.Keys.OrderBy(i => i).ToList();
        int timeOfCurrent = activityTimes.LastOrDefault(t => t <= Game1.timeOfDay);
        int timeOfNext = activityTimes.FirstOrDefault(t => t > Game1.timeOfDay);
        int minutesSinceCurrentStarted = StardewValley.Utility.CalculateMinutesBetweenTimes(timeOfCurrent, Game1.timeOfDay);
        int minutesTillNextStarts = StardewValley.Utility.CalculateMinutesBetweenTimes(Game1.timeOfDay, timeOfNext);
        int timeOfActivity;

        if (timeOfCurrent == 0) // Means it's the start of the day
            timeOfActivity = activityTimes.First();
        else if (timeOfNext == 0) // Means it's the end of the day
            timeOfActivity = activityTimes.Last();
        else
        {
            // If we're very close to the next item, 
            if (minutesTillNextStarts < minutesSinceCurrentStarted && minutesTillNextStarts <= 30)
                timeOfActivity = timeOfNext;
            else
                timeOfActivity = timeOfCurrent;
        }

        return timeOfActivity;
    }
}
