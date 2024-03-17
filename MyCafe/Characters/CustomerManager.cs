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
using MonsoonSheep.Stardew.Common;
using MyCafe.Data.Models;
using StardewValley.Pathfinding;
using System.Text.RegularExpressions;

namespace MyCafe.Characters;

internal sealed class CustomerManager
{
    internal List<CustomerGroup> ActiveGroups = [];

    internal readonly Dictionary<string, VillagerCustomerData> VillagerData = new();

    internal CustomerManager()
    {
        foreach (var model in Mod.Assets.VillagerVisitors)
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
        List<NPC>? npcs = this.CreateRandomCustomerGroup(Game1.random.Next(1, table.Seats.Count + 1));
        if (npcs == null)
        {
            Log.Debug("No random customers can be created.");
            return null;
        }

        CustomerGroup group = new CustomerGroup(npcs);

        if (group.ReserveTable(table) == false)
        {
            Log.Debug("Couldn't reserve table for random customers");
            this.EndCustomers(group, force: true);
            return null;
        }
        
        foreach (NPC c in group.Members)
            c.get_OrderItem().Set(Debug.SetTestItemForOrder(c));

        GameLocation busStop = Game1.getLocationFromName("BusStop");
        foreach (NPC c in group.Members)
        {
            busStop.addCharacter(c);
            c.Position = new Vector2(33, 9) * 64;
        }
        
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

        List<NPC> npcs = data.Select(d => d.Npc).ToList();

        CustomerGroup group = new CustomerGroup(npcs);

        if (group.ReserveTable(table) == false)
            return null;

        foreach (NPC c in group.Members)
            c.get_OrderItem().Set(Debug.SetTestItemForOrder(c));

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

    internal VillagerCustomerData[] GetAvailableVillagerCustomers(int count)
    {
        foreach (KeyValuePair<string, VillagerCustomerData> data in this.VillagerData.OrderBy(_ => Game1.random.Next()))
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

    internal List<NPC>? CreateRandomCustomerGroup(int count)
    {
        List<CustomerModel> models = [];

        for (int i = 0; i < count; i++)
        {
            CustomerModel model = Mod.CharacterFactory.CreateRandomCustomer(); // Generate from CharGen
            models.Add(model);
        }

        if (!models.Any())
            return null;

        List<NPC> customers = [];
        foreach (CustomerModel model in models)
        {
            Texture2D portrait = Game1.content.Load<Texture2D>(model.Portrait);
            AnimatedSprite sprite = new AnimatedSprite(model.Spritesheet, 0, 16, 32);
            NPC c = new NPC(sprite, new Vector2(10, 12) * 64f, "BusStop", 2, model.Name, false, portrait);
            customers.Add(c);
        }

        return customers;
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
                c.Delete();
        }
        else
        {
            try
            {
                group.MoveTo(
                    Game1.getLocationFromName("BusStop"),
                    new Point(33, 9),
                    (c, loc) => (c as NPC)!.Delete());
            }
            catch (PathNotFoundException e)
            {
                Log.Error($"Couldn't return customers to bus stop\n{e.Message}\n{e.StackTrace}");
                this.EndCustomers(group, force: true);
            }
        }
    }

    internal void RemoveAllCustomers()
    {
        for (int i = this.ActiveGroups.Count - 1; i >= 0; i--)
            this.EndCustomers(this.ActiveGroups[i], force: true);
    }

    internal void TryRemoveRandomNpcData(string name)
    {
        Match findRandomGuid = new Regex($@"{ModKeys.CUSTOMER_NPC_NAME_PREFIX}Random(.*)").Match(name);
        if (findRandomGuid.Success)
        {
            Log.Trace("Deleting random customer and its generated sprite");
            string guid = findRandomGuid.Groups[1].Value;
            if (Mod.Cafe.GeneratedSprites.Remove(guid) == false)
                Log.Trace("Tried to remove GUID for random customer but it wasn't registered.");
        }
    }
}
