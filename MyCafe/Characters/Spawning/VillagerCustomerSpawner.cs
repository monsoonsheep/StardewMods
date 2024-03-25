using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using MyCafe.Data.Customers;
using MyCafe.Data.Models;
using MyCafe.Enums;
using MyCafe.Locations.Objects;
using StardewModdingAPI;
using StardewValley;
using SUtility = StardewValley.Utility;

namespace MyCafe.Characters.Spawning;
internal class VillagerCustomerSpawner : CustomerSpawner
{
    internal readonly Dictionary<string, VillagerCustomerData> VillagerData = new();

    internal override void Initialize(IModHelper helper)
    {
        foreach (var model in Mod.Assets.VillagerCustomerModels)
        {
            VillagerCustomerData data = new VillagerCustomerData(model.Value.NpcName);
            this.VillagerData[model.Key] = data;
        }
    }

    internal override void DayUpdate()
    {
    }

    internal override bool Spawn(Table table)
    {
        List<VillagerCustomerData> data = this.GetAvailableVillagerCustomers(1);
        if (data.Count == 0)
        {
            Log.Debug("No villager customers can be created");
            return false;
        }

        List<NPC> npcs = data.Select(d => d.GetNpc()).ToList();

        CustomerGroup group = new CustomerGroup(GroupType.Villager, this);
        foreach (NPC npc in npcs)
            group.AddMember(npc);

        if (group.ReserveTable(table) == false)
        {
            return false;
        }

        foreach (NPC c in group.Members)
        {
            c.get_OrderItem().Set(Debug.SetTestItemForOrder(c));
            //c.eventActor = true; // Not doing eventactor, it's a workaround for the NPCBarrier tile property, but we're removing that property now
            c.ignoreScheduleToday = true;
            Mod.Cafe.NpcCustomers.Add(c.Name);
            Log.Trace($"{c.Name} is coming.");
        }

        try
        {
            group.GoToTable();
        }
        catch (PathNotFoundException e)
        {
            Log.Error($"Couldn't make villager customers. Reverting changes...\n{e.Message}\n{e.StackTrace}");

            foreach (NPC c in group.Members)
                c.ReturnToSchedule();

            return false;
        }

        foreach (VillagerCustomerData d in data)
        {
            d.LastVisitedDate = Game1.Date;
        }

        this._groups.Add(group);
        return true;
    }

    internal override bool EndCustomers(CustomerGroup group, bool force = false)
    {
        group.ReservedTable?.Free();
        try
        {
            group.MoveTo(Game1.getLocationFromName("BusStop"), new Point(12, 23), (c, loc) => (c as NPC)!.ReturnToSchedule());
        }
        catch (PathNotFoundException e)
        {
            Log.Error($"Villager NPCs can't find path out of cafe\n{e.Message}\n{e.StackTrace}");
            foreach (NPC npc in group.Members)
            {
                // TODO warp to their home
            }
        }

        this._groups.Remove(group);

        return true;
    }

    private List<VillagerCustomerData> GetAvailableVillagerCustomers(int count)
    {
        List<VillagerCustomerData> list = [];

        foreach (KeyValuePair<string, VillagerCustomerData> data in this.VillagerData.OrderBy(_ => Game1.random.Next()))
        {
            if (list.Count == count)
                break;

            if (this.CanVillagerVisit(data.Value, Game1.timeOfDay))
                list.Add(data.Value);
        }

        return list;
    }

    private bool CanVillagerVisit(VillagerCustomerData data, int timeOfDay)
    {
        NPC npc = data.GetNpc();
        VillagerCustomerModel model = Mod.Assets.VillagerCustomerModels[data.NpcName];

        int daysSinceLastVisit = Game1.Date.TotalDays - data.LastVisitedDate.TotalDays;
        int daysAllowed = model.VisitFrequency switch
        {
            0 => 200,
            1 => 27,
            2 => 13,
            3 => 7,
            4 => 3,
            5 => 1,
            _ => 9999999
        };

        if (Mod.Cafe.NpcCustomers.Contains(data.NpcName) ||
            npc.isSleeping.Value == true ||
            npc.ScheduleKey == null ||
            daysSinceLastVisit < daysAllowed)
            return false;

        // If no busy period for today, they're free all day
        if (!model.BusyTimes.TryGetValue(npc.ScheduleKey, out List<BusyPeriod>? busyPeriods))
            return false;
        if (busyPeriods.Count == 0)
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

 
}
