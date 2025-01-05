using System.Text.Json.Serialization;
using System.Xml.Serialization;
using StardewMods.FoodJoints.Framework.Data.Models;

namespace StardewMods.FoodJoints.Framework.Data;

/// <summary>
/// Keeps track of the villager as a customer. 
/// </summary>
public class VillagerCustomerData
{
    internal string NpcName { get; set; } = null!;
    public WorldDate LastVisitedDate = new(1, Season.Spring, 1);
    public string? LastAteFood;

    private List<(int, int)>? freePeriods;

    [JsonIgnore]
    [XmlIgnore]
    internal List<(int, int)>? FreePeriods {
        get
        {
            if (this.freePeriods == null)
            {
                NPC npc = this.GetNpc();
                VillagerCustomerModel model = Mod.Customers.VillagerCustomerModels[this.NpcName];

                this.freePeriods = new List<(int, int)>();

                // If there's no busytimes data, no visits
                if (model.BusyTimes.TryGetValue(npc.ScheduleKey, out List<BusyPeriod>? busyPeriods))
                {
                    // If no busy period for today, they're free all day
                    if (busyPeriods.Count == 0)
                    {
                        this.freePeriods.Add((600, 2300));
                    }
                    else
                    {
                        int cursor = 600;
                        // Check their busy periods for their current schedule key
                        foreach (BusyPeriod busyPeriod in busyPeriods)
                        {
                            // if free period length is 100 mins or more, add period from cursor to m1
                            if (Utility.CalculateMinutesBetweenTimes(cursor, busyPeriod.From) > 100)
                            {
                                this.freePeriods.Add((cursor, busyPeriod.From));
                            }

                            // move cursor to end of busy period
                            cursor = busyPeriod.To;
                        }
                    }
                }
            }

            return this.freePeriods;
        }
    }

    public VillagerCustomerData()
    {

    }

    public VillagerCustomerData(string name)
    {
        this.NpcName = name;
    }

    internal NPC GetNpc()
    {
        return Game1.getCharacterFromName(this.NpcName);
    }
}

