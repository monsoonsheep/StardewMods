using System;
using MyCafe.Game;
using MyCafe.Locations.Objects;
using StardewValley;

namespace MyCafe.Characters.Factory;

internal abstract class CustomerBuilder
{
    protected Table? _table;
    protected CustomerGroup? _group;

    internal virtual bool SetupGroup()
    {
        foreach (NPC npc in this._group!.Members)
        {
            Item? item = Mod.Cafe.GetMenuItemForCustomer(npc);
            if (item == null)
                return false;

            npc.get_OrderItem().Set(item);
        }

        return true;
    }

#nullable disable
    internal bool ReserveTable()
    {
        if (this._group!.ReserveTable(this._table!) == false)
        {
            Log.Error("Couldn't reserve table for customers");
            return false;
        }

        return true;
    }

    internal abstract bool PreMove();
    internal abstract bool MoveToTable();
    internal abstract bool PostMove();
    internal abstract void RevertChanges();

    internal abstract CustomerGroup GenerateGroup();

    internal CustomerGroup TrySpawnRandom(Table table)
    {
        CustomerGroup group = this.GenerateGroup();
        if (group == null)
            return null;

        return this.TrySpawn(group, table);
    }

    internal CustomerGroup TrySpawn(CustomerGroup group, Table table)
    {
        this._group = group;
        this._table = table;

        if (this.DoSpawnSteps() == false)
        {
            Log.Trace("Error spawning customers");
            this.RevertChanges();
            return null;
        }

        return this._group;
    }

    private bool DoSpawnSteps()
    {
        return this.SetupGroup() &&
                this.ReserveTable() &&
                this.PreMove() &&
                this.MoveToTable() &&
                this.PostMove();
    }
}
