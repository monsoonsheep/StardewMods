using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Xna.Framework.Graphics;
using MyCafe.Customers;
using StardewModdingAPI;
using MyCafe.Locations.Objects;

namespace MyCafe.CustomerFactory;

internal abstract class CustomerSpawner
{
    internal List<CustomerGroup> ActiveGroups;
    internal SpawnerState State = SpawnerState.Disabled;
    protected Texture2D sprites;

    internal CustomerSpawner(Texture2D sprites)
    {
        this.sprites = sprites;
        ActiveGroups = [];
    }

    internal abstract bool Spawn(Table table, out CustomerGroup groupSpawned);

    internal virtual bool LetGo(CustomerGroup group, bool force = false)
    {
        if (!ActiveGroups.Contains(group))
            return false;
        Log.Debug("Removing group");
        ActiveGroups.Remove(group);
        group.ReservedTable?.Free();
        return true;
    }

    internal abstract void DayUpdate();

    internal abstract Task<bool> Initialize(IModHelper helper);

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