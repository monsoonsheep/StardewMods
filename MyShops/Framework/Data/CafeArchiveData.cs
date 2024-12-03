using System.Collections.Generic;
using StardewValley;
using StardewValley.Inventories;

namespace StardewMods.MyShops.Framework.Data;
public class CafeArchiveData
{
    public int OpeningTime = 900;
    public int ClosingTime = 2100;
    public SerializableDictionary<FoodCategory, Inventory> MenuItemLists = [];
    public SerializableDictionary<string, VillagerCustomerData> VillagerCustomersData = [];
    public SerializableDictionary<string, CustomerData> CustomersData = [];
}
