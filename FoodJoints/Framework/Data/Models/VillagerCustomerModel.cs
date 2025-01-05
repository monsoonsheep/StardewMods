namespace StardewMods.FoodJoints.Framework.Data.Models;
public class VillagerCustomerModel
{
    public string NpcName { get; set; } = null!;

    public Dictionary<string, List<BusyPeriod>> BusyTimes = null!;

    public int VisitFrequency = 2;
}
