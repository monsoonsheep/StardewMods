using StardewMods.FoodJoints.Framework.Game;
using StardewMods.FoodJoints.Framework.Objects;

namespace StardewMods.FoodJoints.Framework.Characters.Factory;

internal abstract class CustomerBuilder
{
    internal Table? _table;
    internal CustomerGroup? _group;

    internal abstract CustomerGroup? GenerateGroup();
    internal abstract bool PreMove();
    internal abstract bool MoveToTable();
    internal abstract bool PostMove();
    internal abstract void Cancel();

    internal CustomerGroup? TrySpawn(Table table, CustomerGroup? group = null)
    {
        this._table = table;
        this._group = group ?? this.GenerateGroup();

        if (this._group == null)
            return null;

        foreach (NPC npc in this._group!.Members)
        {
            Item? item = Mod.Customers.GetMenuItemForCustomer(npc);
            if (item == null)
                return null;

            npc.get_OrderItem().Set(item);
        }

        if (this._group!.ReserveTable(this._table!) == false)
        {
            Log.Error("Couldn't reserve table for customers");
            return null;
        }

        if (this.PreMove() &&
            this.MoveToTable() &&
            this.PostMove())
        {
            return this._group;
        }

        Log.Trace("Error spawning customers");
        this.Cancel();

        return null;
    }
}
