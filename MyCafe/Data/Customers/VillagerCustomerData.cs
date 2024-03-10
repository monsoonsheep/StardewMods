using System.Collections.Generic;
using MyCafe.Data.Models;
using StardewValley;

namespace MyCafe.Data.Customers;

/// <summary>
/// This keeps track of the villager as a customer. 
/// </summary>
public class VillagerCustomerData
{
    internal VillagerCustomerModel Model { get; set; } = null!;
    internal WorldDate LastVisitedDate = new(1, Season.Spring, 1);
}

