using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MyCafe.ChairsAndTables;
using MyCafe.Customers;
using StardewModdingAPI;

namespace MyCafe.Managers;

internal interface ICustomerSpawner
{
    public bool Spawn(Table table, out CustomerGroup groupSpawned);
    public void LetGo(CustomerGroup group);
    public void DayUpdate();
}