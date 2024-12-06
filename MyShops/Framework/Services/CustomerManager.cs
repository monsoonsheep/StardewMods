using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using StardewMods.MyShops.Characters;
using StardewMods.MyShops.Framework.Characters;
using StardewMods.MyShops.Framework.Characters.Factory;
using StardewMods.MyShops.Framework.Data;
using StardewMods.MyShops.Framework.Enums;
using StardewMods.MyShops.Framework.Game;
using StardewMods.MyShops.Framework.Objects;
using StardewValley;
using StardewValley.Pathfinding;
using StardewValley.SpecialOrders.Objectives;

namespace StardewMods.MyShops.Framework.Services;
internal class CustomerManager
{
    internal static CustomerManager Instance = null!;

    private RandomCustomerBuilder randomCustomerBuilder = null!;
    private VillagerCustomerBuilder villagerCustomerBuilder = null!;
    internal readonly List<CustomerGroup> Groups = [];

    internal CustomerManager()
        => Instance = this;

    internal void Initialize()
    {
        this.randomCustomerBuilder = new RandomCustomerBuilder();
        this.villagerCustomerBuilder = new VillagerCustomerBuilder();

        Mod.Events.GameLoop.DayEnding += this.OnDayEnding;
    }

    internal void OnDayEnding(object? sender, DayEndingEventArgs e)
    {
        if (!Context.IsMainPlayer)
            return;

        // Delete customers
        this.RemoveAllCustomers();
        this.CleanUpCustomers();
    }

    internal void CustomerSpawningUpdate()
    {
        // Choose between villager spawn and non-villager spawn
        float weightForVillagers = Mod.Config.EnableNpcCustomers / 5f;
        float weightForRandom = Math.Max(Mod.Config.EnableCustomCustomers, Mod.Config.EnableRandomlyGeneratedCustomers) / 5f;

        float prob = this.ProbabilityToSpawnCustomers(Math.Max(weightForVillagers, weightForRandom));

#if DEBUG
        prob += 0.4f;
        //prob += Debug.ExtraProbabilityToSpawn;
#endif

        Log.Trace($"(chance was {prob})");

        // Try chance
        float random;

        while ((random = Game1.random.NextSingle()) <= prob)
        {
            // REMOVE
            weightForVillagers = 1f;
            weightForRandom = 0f;

            if ((random * (weightForRandom + weightForVillagers)) < weightForVillagers)
                this.SpawnVillagerCustomers();
            else
                this.SpawnRandomCustomers();

            prob -= 0.25f;
        }
    }

    private float ProbabilityToSpawnCustomers(float baseProb)
    {
        int totalTimeIntervalsDuringDay = (Mod.Cafe.ClosingTime - Mod.Cafe.OpeningTime) / 10;
        int minutesTillCloses = StardewValley.Utility.CalculateMinutesBetweenTimes(Game1.timeOfDay, Mod.Cafe.ClosingTime);
        int minutesSinceLastVisitors = StardewValley.Utility.CalculateMinutesBetweenTimes(Mod.Cafe.LastTimeCustomersArrived, Game1.timeOfDay);
        if (minutesTillCloses <= 20)
            return -10.00f;

        float prob = 5f / (float)Math.Pow(Math.Max(totalTimeIntervalsDuringDay - 55, 0), 0.5) * 0.30f;

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

        prob += (Game1.random.NextSingle() * 0.1f);

#if DEBUG
        //prob += Debug.ExtraProbabilityToSpawn;
#endif

        return prob;
    }

    internal void SpawnRandomCustomers()
    {
        Table? table = Mod.Tables.GetFreeTable();
        if (table != null)
        {
            CustomerGroup? group = this.randomCustomerBuilder.TrySpawn(table);
            if (group != null)
                this.AddGroup(group);
        }
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

    internal void RequestNpcCustomer(string name)
    {
        Table? table = Mod.Tables.GetFreeTable();

        if (table != null)
        {
            VillagerCustomerData data = Mod.Instance.VillagerData[name];
            NPC npc = data.GetNpc();

            CustomerGroup group = new CustomerGroup(GroupType.Villager);
            group.AddMember(npc);

            if (this.villagerCustomerBuilder.TrySpawn(table, group) != null)
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

        if (group.Type != GroupType.Villager)
        {
            foreach (NPC c in group.Members)
            {
                if (Mod.Instance.CustomerData.TryGetValue(c.Name, out CustomerData? data))
                {
                    data.LastVisitedData = Game1.Date;
                }
            }

            try
            {
                group.MoveTo(
                    Game1.getLocationFromName("BusStop"),
                    new Point(33, 9),
                    (c, loc) => this.DeleteNpcFromExistence((c as NPC)!));
            }
            catch (Exception e)
            {
                Log.Error($"Couldn't return customers to bus stop\n{e.Message}\n{e.StackTrace}");
                foreach (NPC c in group.Members)
                {
                    this.DeleteNpcFromExistence(c);
                }
            }
        }
        else
        {
            foreach (NPC c in group.Members)
            {
                Mod.Instance.VillagerData[c.Name].LastAteFood = c.get_OrderItem().Value.QualifiedItemId;
                Mod.Instance.VillagerData[c.Name].LastVisitedDate = Game1.Date;
                c.faceTowardFarmerTimer = 0;
                c.faceTowardFarmer = false;
                c.movementPause = 0;
            }

            try
            {
                GameLocation tableLocation = Game1.getLocationFromName(group.ReservedTable!.Location);
                if (tableLocation is Farm || tableLocation.ParentBuilding?.parentLocationName.Value.Equals("Farm") is true)
                {
                    group.MoveTo(
                        Game1.getLocationFromName("BusStop"),
                        new Point(12, 23),
                        (c, _) => this.ReturnVillagerToSchedule((c as NPC)!));
                }
                else
                {
                    foreach (NPC c in group.Members)
                        this.ReturnVillagerToSchedule(c);
                }
            }
            catch (Exception e)
            {
                Log.Error($"Villager NPCs can't find path out of cafe\n{e.Message}\n{e.StackTrace}");
                foreach (NPC npc in group.Members)
                {
                    // TODO warp to their home
                }
            }
        }

    }

    internal void ReturnVillagerToSchedule(NPC npc)
    {
        npc.ignoreScheduleToday = false;
        npc.get_OrderItem().Set(null!);
        npc.set_Seat(null);

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

        SchedulePathDescription originalPathDescription = npc.Schedule[timeOfActivity];

        Log.Trace($"[{Game1.timeOfDay}] Returning {npc.Name} to schedule for key {npc.ScheduleKey}. Time of current activity is {timeOfCurrent}, next activity is at {timeOfNext}.\n" +
                  $"Choosing {timeOfActivity}.\n" +
                  $"Schedule description is {originalPathDescription.targetLocationName}: {originalPathDescription.targetTile}, behavior: {originalPathDescription.endOfRouteBehavior}");

        SchedulePathDescription? route;
        try
        {
            if (npc.currentLocation.Equals(Mod.Locations.Signboard?.Location))
            {
                npc.PathTo(Game1.getLocationFromName(originalPathDescription.targetLocationName), originalPathDescription.targetTile,
                    originalPathDescription.facingDirection);

                route = new SchedulePathDescription(npc.controller.pathToEndPoint, originalPathDescription.facingDirection,
                    originalPathDescription.endOfRouteBehavior, originalPathDescription.endOfRouteMessage,
                    originalPathDescription.targetLocationName, originalPathDescription.targetTile)
                {
                    time = Game1.timeOfDay
                };
                npc.controller = null;
                //npc.set_AfterLerp((object c) => Mod.Cafe.NpcCustomers.Remove(c.Name));
            }
            else
            {
                route = npc.pathfindToNextScheduleLocation(npc.ScheduleKey, npc.currentLocation.Name, npc.TilePoint.X, npc.TilePoint.Y,
                    originalPathDescription.targetLocationName,
                    originalPathDescription.targetTile.X, originalPathDescription.targetTile.Y, originalPathDescription.facingDirection,
                    originalPathDescription.endOfRouteBehavior,
                    originalPathDescription.endOfRouteMessage);
                //this.NpcCustomers.Remove(npc.Name);
            }
        }
        catch (PathNotFoundException e)
        {
            Log.Error($"{e.Message}\n{e.StackTrace}");
            route = null;
        }

        if (route == null)
        {
            //this.NpcCustomers.Remove(npc.Name);
            Log.Error("Couldn't return NPC to schedule");
            // TODO warp them
            return;
        }

        npc.Schedule.Remove(timeOfActivity);
        npc.lastAttemptedSchedule = Game1.timeOfDay - 10;
        npc.queuedSchedulePaths.Clear();
        npc.Schedule[Game1.timeOfDay] = route;
        npc.checkSchedule(Game1.timeOfDay);

        if (npc.controller == null)
        {
            Log.Error("checkSchedule didn't set the controller");
            // TODO WARP
        }
    }

    internal void DeleteNpcFromExistence(NPC npc)
    {
        npc.currentLocation?.characters.Remove(npc);
        Match findRandomGuid = new Regex($@"{Values.CUSTOMER_NPC_NAME_PREFIX}Random(.*)").Match(npc.Name);
        if (findRandomGuid.Success)
        {
            Log.Trace("Deleting random customer and its generated sprite");
            string guid = findRandomGuid.Groups[1].Value;
            //if (this.GeneratedSprites.Remove(guid) == false)
            //    Log.Trace("Tried to remove GUID for random customer but it wasn't registered.");
        }
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
}
