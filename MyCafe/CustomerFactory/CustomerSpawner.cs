using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Xna.Framework.Graphics;
using MyCafe.Customers;
using MyCafe.Enums;
using MyCafe.Locations.Objects;
using StardewModdingAPI;

namespace MyCafe.CustomerFactory;

internal abstract class CustomerSpawner
{
    internal List<CustomerGroup> ActiveGroups;
    internal SpawnerState State = SpawnerState.Disabled;
    protected Texture2D Sprites;

    internal CustomerSpawner(Texture2D sprites)
    {
        this.Sprites = sprites;
        this.ActiveGroups = [];
    }

    internal abstract bool Spawn(Table table, out CustomerGroup groupSpawned);

    internal virtual bool LetGo(CustomerGroup group, bool force = false)
    {
        if (!this.ActiveGroups.Contains(group))
            return false;
        Log.Debug("Removing group");
        this.ActiveGroups.Remove(group);
        group.ReservedTable?.Free();
        return true;
    }

    internal abstract void DayUpdate();

    internal abstract Task<bool> Initialize(IModHelper helper);

    internal virtual void RemoveAll()
    {
        for (int i = this.ActiveGroups.Count - 1; i >= 0; i--)
        {
            this.LetGo(this.ActiveGroups[i]);
        }
    }
}
