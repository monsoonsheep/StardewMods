using MyCafe.ChairsAndTables;
using MyCafe.Customers;
using StardewModdingAPI;

namespace MyCafe.CustomerProduction;

public interface ICustomerSpawner
{
    public bool Spawn(Table table, out CustomerGroup groupSpawned);
    public void LetGo(CustomerGroup group);
    public void DayUpdate();
    public void Initialize(IModHelper helper);
}