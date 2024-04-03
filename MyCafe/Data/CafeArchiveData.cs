using System.Collections.Generic;
using MyCafe.Data.Customers;
using MyCafe.Inventories;
using StardewValley;
using StardewValley.Inventories;

namespace MyCafe.Data;
public class CafeArchiveData
{
    public int OpeningTime = 900;
    public int ClosingTime = 2100;
    public SerializableDictionary<FoodCategory, Inventory> MenuItemLists = [];
    public List<VillagerCustomerData> VillagerCustomersData = [];
}
