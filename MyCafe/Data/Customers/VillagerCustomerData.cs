using System.Collections.Generic;
using System.Xml.Serialization;
using Microsoft.Xna.Framework;
using MyCafe.Data.Models;
using StardewValley;
using StardewValley.Pathfinding;

namespace MyCafe.Data.Customers;

/// <summary>
/// Keeps track of the villager as a customer. 
/// </summary>
public class VillagerCustomerData
{
    public string NpcName { get; set; } = null!;
    public WorldDate LastVisitedDate = new(1, Season.Spring, 1);

    [XmlIgnore]
    internal VillagerCustomerModel Model { get; set; } = null!;

    private NPC? npc;

    [XmlIgnore]
    internal NPC Npc => this.npc ??= Game1.getCharacterFromName(this.Model.NpcName);

    public VillagerCustomerData()
    {

    }

    public VillagerCustomerData(VillagerCustomerModel model)
    {
        this.NpcName = model.NpcName;
        this.Model = model;
    }
}

