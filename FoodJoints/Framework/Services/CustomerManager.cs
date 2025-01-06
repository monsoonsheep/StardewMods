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

    internal DynamicScheduler dynamicScheduler = null!;
    internal VillagerScheduler villagerScheduler = null!;

    internal CustomerManager()
        => Instance = this;

    internal void Initialize()
    {
        this.villagerCustomerBuilder = new VillagerCustomerBuilder();
        this.dynamicScheduler = new DynamicScheduler();
        this.villagerScheduler = new VillagerScheduler();

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

    internal void RandomSpawningUpdate()
    {
        int count = this.dynamicScheduler.TrySpawn();
        count = 0;
        for (int i = 0; i < count; i++)
            this.SpawnCustomers(CustomerGroupType.Random);
    }

    internal void SpawnCustomers(CustomerGroupType type)
    {
        Table? table = Mod.Tables.GetFreeTable();
        if (table != null)
        {
            CustomerGroup? group = this.villagerCustomerBuilder.TrySpawn(table);
            if (group != null)
            {
                this.Groups.Add(group);
                Mod.Cafe.LastTimeCustomersArrived = Game1.timeOfDay;
            }
        }
    }

    internal Item? GetMenuItemForCustomer(NPC npc)
    {
        SortedDictionary<int, List<Item>> tastesItems = [];
        int count = 0;

        foreach (Item i in Mod.Cafe.Menu.ItemDictionary.Values.SelectMany(i => i))
        {
            GiftObjective.LikeLevels tasteLevel;
            switch (npc.getGiftTasteForThisItem(i))
            {
                case 6:
                    tasteLevel = GiftObjective.LikeLevels.Hated;
                    break;
                case 4:
                    tasteLevel = GiftObjective.LikeLevels.Disliked;
                    break;
                case 8:
                    tasteLevel = GiftObjective.LikeLevels.Neutral;
                    break;
                case 2:
                    tasteLevel = GiftObjective.LikeLevels.Liked;
                    break;
                case 0:
                    tasteLevel = GiftObjective.LikeLevels.Loved;
                    break;
                default:
                    continue;
            }

            if (!tastesItems.ContainsKey((int) tasteLevel))
                tastesItems[(int) tasteLevel] = [];

            tastesItems[(int) tasteLevel].Add(i);
            count++;
        }

        // Null if either no items on menu or the best the menu can do for the npc is less than neutral taste for them
        if (count == 0 || tastesItems.Keys.Max() <= (int) GiftObjective.LikeLevels.Disliked)
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
