using System.Text.Json.Serialization;
using System.Xml.Serialization;
using StardewValley;

namespace StardewMods.FoodJoints.Framework.Data;

/// <summary>
/// Keeps track of the villager as a customer. 
/// </summary>
public class VillagerCustomerData
{
    internal string NpcName { get; set; } = null!;
    public WorldDate LastVisitedDate = new(1, Season.Spring, 1);
    public string? LastAteFood;

    [JsonIgnore]
    [XmlIgnore]
    internal List<(int, int)>? FreePeriods { get; set; }

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

