using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MyCafe.Framework.ChairsAndTables;
using MyCafe.Framework.Customers;

namespace MyCafe.Framework.Managers;

internal interface ICustomerSpawner
{
    public bool Spawn(Table table, out CustomerGroup groupSpawned);
    public void LetGo(CustomerGroup group);
    public void DayUpdate();
}