using System.Collections.Generic;
using Monsoonsheep.StardewMods.MyCafe.Data.Customers;
using Monsoonsheep.StardewMods.MyCafe.Inventories;
using StardewValley;
using StardewValley.Inventories;

namespace Monsoonsheep.StardewMods.MyCafe.Data;
public class CafeArchiveData
{
    public int OpeningTime = 900;
    public int ClosingTime = 2100;
    public SerializableDictionary<FoodCategory, Inventory> MenuItemLists = [];
    public SerializableDictionary<string, VillagerCustomerData> VillagerCustomersData = [];
    public SerializableDictionary<string, CustomerData> CustomersData = [];
}
