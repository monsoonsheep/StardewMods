using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Xna.Framework.Graphics;
using MyCafe;
using MyCafe.Characters;
using MyCafe.Enums;
using MyCafe.Locations.Objects;
using StardewModdingAPI;

namespace MyCafe.Characters.Spawning;

internal abstract class CustomerSpawnerBase
{
    internal List<CustomerGroup> ActiveGroups;
    internal SpawnerState State = SpawnerState.Disabled;

    internal CustomerSpawnerBase()
    {
        this.ActiveGroups = [];
    }

    internal abstract bool Spawn(Table table, out CustomerGroup groupSpawned);

    internal virtual bool LetGo(CustomerGroup group, bool force = false)
    {
        if (!this.ActiveGroups.Contains(group))
            return false;
        Log.Debug("Removing group");
        group.ReservedTable?.Free();
        this.ActiveGroups.Remove(group);
        return true;
    }

    internal abstract void DayUpdate();

    internal virtual void Initialize(IModHelper helper)
    {
        this.State = SpawnerState.Enabled;
    }

    internal virtual void RemoveAll()
    {
        for (int i = this.ActiveGroups.Count - 1; i >= 0; i--)
        {
            this.LetGo(this.ActiveGroups[i]);
        }
    }
}
