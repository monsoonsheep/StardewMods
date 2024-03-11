using System.Collections.Generic;
using Microsoft.Xna.Framework;
using MyCafe.Data.Models;
using StardewValley;
using StardewValley.Pathfinding;

namespace MyCafe.Data.Customers;

/// <summary>
/// This keeps track of the villager as a customer. 
/// </summary>
public class VillagerCustomerData
{
    public VillagerCustomerModel Model { get; set; } = null!;
    public WorldDate LastVisitedDate = new(1, Season.Spring, 1);

    private NPC? npc;
    internal NPC Npc => this.npc ??= Game1.getCharacterFromName(this.Model.NpcName);

    internal PathFindController? PreviousController;
    internal GameLocation? PreviousLocation;
    internal Vector2 PreviousPosition;
    internal int PreviousFacingDirection;

    public VillagerCustomerData()
    {

    }

    public VillagerCustomerData(VillagerCustomerModel model)
    {
        this.Model = model;
    }
}

