using System.Collections.Generic;
using MyCafe.ChairsAndTables;
using MyCafe.Customers;
using StardewModdingAPI;

namespace MyCafe.CustomerFactory;

internal abstract class CustomerSpawner
{
    internal List<CustomerGroup> ActiveGroups;

    internal CustomerSpawner()
    {
        ActiveGroups = [];
    }
    internal abstract bool Spawn(Table table, out CustomerGroup groupSpawned);

    internal virtual void LetGo(CustomerGroup group)
    {
        Log.Debug("Removing group");
        ActiveGroups.Remove(group);
        group.ReservedTable.Free();
    }

    internal abstract void DayUpdate();
    internal abstract void Initialize(IModHelper helper);

    internal virtual void RemoveAll()
    {
        foreach (var group in ActiveGroups)
            LetGo(group);
    }
}