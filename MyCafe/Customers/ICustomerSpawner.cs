using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MyCafe.ChairsAndTables;
using StardewModdingAPI;

namespace MyCafe.Customers;

public interface ICustomerSpawner
{
    public bool Spawn(Table table, out CustomerGroup groupSpawned);
    public void LetGo(CustomerGroup group);
    public void DayUpdate();
    public void Initialize(IModHelper helper);
}