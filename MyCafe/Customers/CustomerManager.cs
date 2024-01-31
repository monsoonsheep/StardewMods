using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework.Graphics;
using MyCafe.CustomerFactory;
using MyCafe.Customers.Data;
using MyCafe.Locations.Objects;
using MyCafe.Enums;
using StardewModdingAPI;

namespace MyCafe.Customers;

internal sealed class CustomerManager
{
    internal Cafe Cafe;

    internal CustomerSpawner BusCustomers;
    internal CustomerSpawner VillagerCustomers;
    internal CustomerSpawner? ChatCustomers = null!;

    internal IEnumerable<CustomerGroup> CurrentGroups
    {
        get
        {
            var l = this.BusCustomers.ActiveGroups.Concat(this.VillagerCustomers.ActiveGroups);
            return this.ChatCustomers == null ? l : l.Concat(this.ChatCustomers.ActiveGroups);
        }
    }

    internal CustomerManager(IModHelper helper, Dictionary<string, BusCustomerData> customersData, Texture2D sprites, Cafe cafe)
    {
        this.BusCustomers = new BusCustomerSpawner(customersData, sprites);
        this.VillagerCustomers = new VillagerCustomerSpawner(sprites);

        this.BusCustomers.Initialize(helper);
        this.VillagerCustomers.Initialize(helper);
        this.Cafe = cafe;

#if YOUTUBE || TWITCH
        ChatCustomers = new ChatCustomerSpawner();
#endif
    }

    internal void DayUpdate()
    {
        this.VillagerCustomers.DayUpdate();
        this.BusCustomers.DayUpdate();
        this.ChatCustomers?.DayUpdate();
    }

    internal void SpawnCustomers()
    {
        Table? table = this.Cafe.Tables.Where(t => !t.IsReserved).MinBy(t => t.Seats.Count);
        if (table == null)
        {
            Log.Debug("No tables available");
            return;
        }

        if (this.ChatCustomers is { State: SpawnerState.Enabled })
        {
            if (this.ChatCustomers.Spawn(table, out _) is true)
            {
                return;
            }
        }

        if (this.BusCustomers.Spawn(table, out _) is true)
        {
            return;
        }
    }

    internal void RemoveAllCustomers()
    {
        this.BusCustomers.RemoveAll();
        this.VillagerCustomers.RemoveAll();
        this.ChatCustomers?.RemoveAll();
    }

    internal CustomerGroup? GetGroupFromTable(Table table)
    {
        return this.CurrentGroups.FirstOrDefault(g => g.ReservedTable == table);
    }

    internal void LetGo(CustomerGroup group, bool force = false)
    {
        if (this.BusCustomers.ActiveGroups.Contains(group))
            this.BusCustomers.LetGo(group, force);
        else if (this.VillagerCustomers.ActiveGroups.Contains(group))
            this.VillagerCustomers.LetGo(group, force);
        else if (this.ChatCustomers?.ActiveGroups.Contains(group) ?? false)
            this.ChatCustomers.LetGo(group, force);
        else
        {
            Log.Error("Group not found, couldn't be deleted. This is a bug.");
        }
    }
}
