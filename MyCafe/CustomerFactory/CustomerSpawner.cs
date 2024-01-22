using System.Collections.Generic;
using MyCafe.Locations;
using MyCafe.Customers;
using StardewModdingAPI;

namespace MyCafe.CustomerFactory;

internal abstract class CustomerSpawner
{
    internal List<CustomerGroup> ActiveGroups;
    internal SpawnerState State = SpawnerState.Disabled;

    internal CustomerSpawner()
    {
        ActiveGroups = [];
    }

    internal abstract bool Spawn(Table table, out CustomerGroup groupSpawned);

    internal virtual bool LetGo(CustomerGroup group, bool force = false)
    {
        if (group == null || !ActiveGroups.Contains(group))
            return false;
        Log.Debug("Removing group");
        ActiveGroups.Remove(group);
        group.ReservedTable.Free();
        return true;
    }

    internal abstract void DayUpdate();

    internal abstract void Initialize(IModHelper helper);

    internal virtual void RemoveAll()
    {
        for (int i = ActiveGroups.Count - 1; i >= 0; i--)
        {
            LetGo(ActiveGroups[i]);
        }
    }
}

internal enum SpawnerState
{
    Disabled, Initializing, Enabled
}