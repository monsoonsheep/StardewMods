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
            this.VillagerData[model.Key] = new VillagerCustomerData(model.Value);
            // TODO Load from save (pass in thru constructor)
        }
    }

    internal void DayUpdate()
    {
        
    }

    internal void TrySpawnGrouop(Table table, GroupType type)
    {
        CustomerGroup? group = null;

        switch (type)
        {
            case GroupType.Random:
                group = this.SpawnRandomCustomers(table);
                break;
            case GroupType.Villager:
                group = this.SpawnVillagerCustomers(table);
                break;
            case GroupType.LiveChat:
                break;
        }

        if (group != null)
            this.ActiveGroups.Add(group);
    }

    internal CustomerGroup? SpawnRandomCustomers(Table table)
    {
        List<NPC>? npcs = this.CreateRandomCustomerGroup(1);
        if (npcs == null)
        {
            Log.Debug("No random customers can be created.");
            return null;
        }

        CustomerGroup group = new CustomerGroup(npcs);

        if (group.ReserveTable(table) == false)
        {
            Log.Debug("Couldn't reserve table for random customers");
            return null;
        }
        
        foreach (NPC c in group.Members)
            c.get_OrderItem().Set(this.SetTestItemForOrder(c));

        group.AddToBusStop();

        try
        {
            group.GoToTable();
        }
        catch (PathNotFoundException e)
        {
            // TODO: Return NPC to schedule
            Log.Error($"Couldn't spawn random customers: {e.Message}\n{e.StackTrace}");
            this.EndCustomers(group, force: true);
            return null;
        }

        return group;
    }

    internal CustomerGroup? SpawnVillagerCustomers(Table table)
    {
        VillagerCustomerData[] data = this.GetAvailableVillagerCustomers(1);
        if (data.Length == 0)
        {
            Log.Debug("No villager customers can be created");
            return null;
        }

        List<NPC> npcs = [];
        foreach (VillagerCustomerData d in data)
        {
            NPC? n = d.Npc;
            if (n == null)
            {
                Log.Error($"Villager data can't get real NPC for {d.Model.NpcName}");
                return null;
            }

            npcs.Add(n);
        }

        CustomerGroup group = new CustomerGroup(npcs);

        if (group.ReserveTable(table) == false)
            return null;

        foreach (NPC c in group.Members)
            c.get_OrderItem().Set(this.SetTestItemForOrder(c));

        try
        {
            group.GoToTable();
        }
        catch (PathNotFoundException e)
        {
            Log.Error($"Couldn't make villager customers. Reverting changes...\n{e.Message}\n{e.StackTrace}");

            foreach (NPC c in group.Members)
                c.ReturnToSchedule();

            return null;
        }
        
        return group;
    }

    internal List<NPC>? CreateRandomCustomerGroup(int count)
    {
        List<CustomerModel> models = [];

        for (int i = 0; i < count; i++)
        {
            CustomerModel? model;

            if (Game1.random.Next(4) == 1)
                model = this._assetManager.Customers.Values.Where(m => !models.Contains(m)).MinBy(_ => Game1.random.Next());
            else
                model = null;
        }

        if (!models.Any())
            return null;

        List<NPC> customers = [];
        foreach (CustomerModel model in models)
        {
            try
            {
                Texture2D portrait = Game1.content.Load<Texture2D>(model.Portrait);
                AnimatedSprite sprite = new AnimatedSprite(model.Spritesheet, 0, 16, 32);
                NPC c = new NPC(sprite, new Vector2(10, 12) * 64f, "BusStop", 2, $"CustomerNPC_{model.Name}", false, portrait);
                customers.Add(c);
            }
            catch (Exception e)
            {
                Log.Error($"Error creating character {model.Name}: {e.Message}\n{e.StackTrace}");
                return null;
            }
        }

        return customers;
    }

    internal Item SetTestItemForOrder(NPC customer)
    {
        return ItemRegistry.Create<StardewValley.Object>("(O)128");
    }

    
    internal VillagerCustomerData[] GetAvailableVillagerCustomers(int count)
    {
        foreach (var data in this.VillagerData.OrderBy(_ => Game1.random.Next()))
        {
            NPC npc = data.Value.Npc;

            if (this.CanVillagerVisit(npc, Game1.timeOfDay))
            {
                
            }
        }

        return [];
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

    
    internal void EndCustomers(CustomerGroup group, bool force = false)
    {
        Log.Debug($"Removing customers{(force ? " By force" : "")}");
        group.ReservedTable?.Free();
        this.ActiveGroups.Remove(group);
        
        // Random
        if (force)
        {
            foreach (NPC c in group.Members)
                c.currentLocation.characters.Remove(c);
        }
        else
        {
            try
            {
                group.MoveTo(
                    Game1.getLocationFromName("BusStop"),
                    new Point(33, 9),
                    (c, loc) => loc.characters.Remove(c as NPC));
            }
            catch (PathNotFoundException e)
            {
                Log.Error("Couldn't return customers to bus stop");
                foreach (NPC c in group.Members)
                    c.currentLocation.characters.Remove(c);
            }
        }
    }

    internal void RemoveAllCustomers()
    {
        for (int i = this.ActiveGroups.Count - 1; i >= 0; i--)
        {
            this.EndCustomers(this.ActiveGroups[i]);
        }
    }
}
