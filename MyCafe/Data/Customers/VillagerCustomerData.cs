using StardewValley;

namespace MyCafe.Data.Customers;

/// <summary>
/// Keeps track of the villager as a customer. 
/// </summary>
public class VillagerCustomerData
{
    public string NpcName { get; set; } = null!;
    public WorldDate LastVisitedDate = new(1, Season.Spring, 1);
    public string? LastAteFood;


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

