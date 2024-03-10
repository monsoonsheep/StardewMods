using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework.Graphics;
using MyCafe;
using MyCafe.Locations.Objects;
using MyCafe.Enums;
using StardewModdingAPI;
using MyCafe.Data.Customers;
using StardewValley;
using System.Globalization;
using System;
using MyCafe.Interfaces;
using SUtility = StardewValley.Utility;
using HarmonyLib;
using Microsoft.Xna.Framework;
using MyCafe.Data.Models;
using StardewValley.Pathfinding;

namespace MyCafe.Characters;

internal sealed class CustomerManager
{
    internal List<CustomerGroup> ActiveGroups = [];
    private readonly AssetManager _assetManager;

    internal readonly Dictionary<string, VillagerCustomerData> VillagerData = new();

    internal CustomerManager(AssetManager assets)
    {
        this._assetManager = assets;
        foreach (var model in this._assetManager.VillagerVisitors)
        {
            this.VillagerData[model.Key] = new VillagerCustomerData()
            {
                Model = model.Value
            };
        }
    }

    internal void DayUpdate()
    {
        
    }

    internal CustomerGroup? SpawnRandomCustomerGroup(Table table)
    {
        CustomerGroup? group = this.GetRandomCustomerGroup();
        if (group == null)
            return null;

        if (group.ReserveTable(table) == false)
            return null;
        
        foreach (NPC c in group.Members)
            c.get_OrderItem().Set(this.SetTestItemForOrder(c));

        return group;
    }

    internal CustomerGroup? SpawnVillagerCustomerGroup(Table table)
    {
        foreach (var data in this.VillagerData.OrderBy(_ => Game1.random.Next()))
        {
            NPC npc = Game1.getCharacterFromName(data.Key);
            if (this.CanVillagerVisit(npc, Game1.timeOfDay))
            {
                
            }
        }

        return null;
    }

    internal bool SpawnCustomerGroup(Table table, Func<CustomerGroup?> getGroupFunc, Func<NPC, Item> setOrderItemFunc, Func<CustomerGroup, bool> initiateCustomersFunc, out CustomerGroup? group)
    {
        group = getGroupFunc.Invoke();
        if (group == null || group.ReserveTable(table) == false)
        {
            Log.Error("Table couldn't be reserved. Bug!");
            return false;
        }

        foreach (NPC c in group.Members)
            c.get_OrderItem().Set(setOrderItemFunc.Invoke(c));

        if (initiateCustomersFunc.Invoke(group) == false)
            return false;

        Log.Debug("Customers are coming");
        table.State.Set(TableState.CustomersComing);
        this.ActiveGroups.Add(group);
        return true;
    }

    internal List<CustomerModel> GetRandomCustomerModels(int count)
    {
        return this._assetManager.Customers.Values.OrderBy(x => Game1.random.Next()).Take(count).ToList();
    }

    internal static NPC CreateCustomerFromModel(CustomerModel model)
    {
        Texture2D portrait = Game1.content.Load<Texture2D>(model.Portrait);
        AnimatedSprite sprite = new AnimatedSprite(model.Spritesheet, 0, 16, 32);
        return new NPC(sprite, new Vector2(10, 12) * 64f, "BusStop", 2, $"CustomerNPC_{model.Name}", false, portrait);
    }

    internal CustomerGroup? GetRandomCustomerGroup()
    {
        List<CustomerModel> models = this.GetRandomCustomerModels(1);
        if (!models.Any())
            return null;
        List<NPC> customers = [];
        foreach (CustomerModel model in models)
        {
            try
            {
                NPC c = CreateCustomerFromModel(model);
                customers.Add(c);
            }
            catch (Exception e)
            {
                Log.Error($"Error creating character {model.Name}: {e.Message}\n{e.StackTrace}");
                return null;
            }
        }

        return new CustomerGroup(customers);
    }

    internal Item SetTestItemForOrder(NPC customer)
    {
        return ItemRegistry.Create<StardewValley.Object>("(O)128");
    }

    internal bool AddCustomersToBusStop(CustomerGroup group)
    {
        GameLocation busStop = Game1.getLocationFromName("BusStop");
        foreach (NPC c in group.Members)
        {
            busStop.addCharacter(c);
            c.Position = new Vector2(33, 9) * 64;
        }

        if (group.MoveToTable() is false)
        {
            Log.Error("Customers couldn't path to cafe");
            this.LetGo(group, force: true);
            return false;
        }

        return true;
    }

    internal bool LetGo(CustomerGroup group, bool force = false)
    {
        if (!this.ActiveGroups.Contains(group))
            return false;

        Log.Debug("Removing group");
        group.ReservedTable?.Free();
        this.ActiveGroups.Remove(group);

        // Random
        if (force)
        {
            group.Delete();
        }
        else
        {
            group.MoveTo(
            Game1.getLocationFromName("BusStop"),
            new Point(33, 9),
            (c, loc) => loc.characters.Remove(c as NPC));
        }

        return true;

        // if villager
        //Customer v = group.Members.First();
        //NPC original = this.VillagerData[v.Name].RealNpc;

        //if (original != null)
        //{
        //    original.currentLocation = v.currentLocation;
        //    original.Position = v.Position;
        //    original.TryLoadSchedule(v.ScheduleKey);
        //    original.faceDirection(v.FacingDirection);
        //    original.ignoreScheduleToday = false;

        //    var activityTimes = v.Schedule.Keys.OrderBy(i => i).ToList();
        //    int timeOfCurrent = activityTimes.LastOrDefault(t => t <= Game1.timeOfDay);
        //    int timeOfNext = activityTimes.FirstOrDefault(t => t > Game1.timeOfDay);
        //    int minutesSinceCurrentStarted = SUtility.CalculateMinutesBetweenTimes(timeOfCurrent, Game1.timeOfDay);
        //    int minutesTillNextStarts = SUtility.CalculateMinutesBetweenTimes(Game1.timeOfDay, timeOfNext);
        //    int timeOfActivity;
        //    if (timeOfCurrent == 0) // Means it's the start of the day
        //    {
        //        timeOfActivity = activityTimes.First();
        //    }
        //    else if (timeOfNext == 0) // Means it's the end of the day
        //    {
        //        timeOfActivity = activityTimes.Last();
        //    }
        //    else
        //    {
        //        if (minutesTillNextStarts < minutesSinceCurrentStarted && minutesTillNextStarts <= 30)
        //            // If we're very close to the next item, 
        //            timeOfActivity = timeOfNext;
        //        else
        //            timeOfActivity = timeOfCurrent;

        //    }

        //    SchedulePathDescription originalPathDescription = original.Schedule[timeOfActivity];

        //    GameLocation targetLocation = Game1.getLocationFromName(originalPathDescription.targetLocationName);
        //    if (targetLocation != null)
        //    {
        //        Stack<Point>? routeToScheduleItem = Pathfinding.PathfindFromLocationToLocation(
        //            original.currentLocation,
        //            original.TilePoint,
        //            targetLocation,
        //            originalPathDescription.targetTile,
        //            original);

        //        SchedulePathDescription toInsert = new SchedulePathDescription(
        //            routeToScheduleItem,
        //            originalPathDescription.facingDirection,
        //            originalPathDescription.endOfRouteBehavior,
        //            originalPathDescription.endOfRouteMessage,
        //            targetLocation.Name,
        //            originalPathDescription.targetTile)
        //        {
        //            time = Game1.timeOfDay
        //        };

        //        original.queuedSchedulePaths.Clear();
        //        original.Schedule[Game1.timeOfDay] = toInsert;
        //        original.checkSchedule(Game1.timeOfDay);
        //    }
        //}

        //v.currentLocation.characters.Remove(v);
        //v.currentLocation.addCharacter(original);
        //return true;
    }

    private bool CanVillagerVisit(NPC npc, int timeOfDay)
    {
        VillagerCustomerData data = this.VillagerData[npc.Name];

        int daysSinceLastVisit = Game1.Date.TotalDays - data.LastVisitedDate.TotalDays;
        int daysBetweenVisits = data.Model.VisitFrequency switch
        {
            0 => 200,
            1 => 28,
            2 => 15,
            3 => 8,
            4 => 2,
            5 => 0,
            _ => 9999999
        };

        if (npc.isSleeping.Value is true ||
            npc.ScheduleKey == null ||
            daysSinceLastVisit <= daysBetweenVisits ||
            data.LastVisitedDate == Game1.Date)
            return false;

        // If no busy period for today, they're free all day
        if (!data.Model.BusyTimes.TryGetValue(npc.ScheduleKey, out List<BusyPeriod>? busyPeriods) || busyPeriods.Count == 0)
            return true;

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

    internal void RemoveAllCustomers()
    {
        for (int i = this.ActiveGroups.Count - 1; i >= 0; i--)
        {
            this.LetGo(this.ActiveGroups[i]);
        }
    }
}
