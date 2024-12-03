using StardewValley;

namespace StardewMods.MyShops.Framework.Data;

/// <summary>
/// Keeps track of the villager as a customer. 
/// </summary>
public class VillagerCustomerData
{
    internal string NpcName { get; set; } = null!;
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

