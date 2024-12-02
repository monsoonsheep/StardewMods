using System;
using Monsoonsheep.StardewMods.MyCafe.Game;
using Monsoonsheep.StardewMods.MyCafe.Locations.Objects;
using StardewValley;

namespace Monsoonsheep.StardewMods.MyCafe.Characters.Factory;

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

    internal abstract CustomerGroup? GenerateGroup();

    internal CustomerGroup? TrySpawn(Table table, CustomerGroup? group = null)
    {
        this._table = table;
        this._group = group ?? this.GenerateGroup();

        if (this._group != null)
            return this.DoSpawnSteps();
        return null;
    }

    private CustomerGroup? DoSpawnSteps()
    {
        if (this.SetupGroup() &&
            this.ReserveTable() &&
            this.PreMove() &&
            this.MoveToTable() &&
            this.PostMove())
        {
            return this._group;
        }

        Log.Trace("Error spawning customers");
        this.RevertChanges();
        return null;
    }
}
