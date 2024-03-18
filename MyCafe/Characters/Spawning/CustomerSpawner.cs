using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MyCafe.Data.Customers;
using MyCafe.Enums;
using MyCafe.Locations.Objects;
using StardewModdingAPI;

namespace MyCafe.Characters.Spawning;
internal abstract class CustomerSpawner
{
    internal readonly List<CustomerGroup> _groups = [];

    internal abstract void Initialize(IModHelper helper);

    internal abstract void DayUpdate();
     
    internal abstract bool Spawn(Table table);

    internal abstract bool EndCustomers(CustomerGroup group, bool force = false);
}
