using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MyCafe.Data.Customers;
using MyCafe.Inventories;
using StardewValley;
using StardewValley.Inventories;

namespace MyCafe.Data;
public class CafeArchiveData
{
    public int OpeningTime = 900;
    public int ClosingTime = 2100;
    public SerializableDictionary<MenuCategory, Inventory> MenuItemLists = [];
    public List<VillagerCustomerData> VillagerCustomersData = [];
}
