using MyCafe.Characters;
using MyCafe.Locations.Objects;

namespace MyCafe;

#nullable disable
internal abstract class CustomerBuilder
{
    protected Table table = null!;
    protected CustomerGroup _group;

    internal CustomerBuilder()
    {
    }

    internal abstract CustomerGroup BuildGroup();
    internal abstract bool SetupGroup();

    internal bool ReserveTable()
    {
        if (this._group.ReserveTable(this.table) == false)
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

    internal CustomerGroup TrySpawn(Table tableToUse)
    {
        this._group = null;
        this.table = tableToUse;
        bool failed = false;

        if (this.SpawnSteps() == false)
        {
            Log.Trace("Error spawning customers");
            this.RevertChanges();
            failed = true;
        }

        if (failed)
        {
            return null;
        }

        return this._group;
    }

    private bool SpawnSteps()
    {
        CustomerGroup g = this.BuildGroup();
        if (g == null)
        {
            this._group = null;
            return false;
        }

        this._group = g;

        return (this.SetupGroup() &&
                this.ReserveTable() &&
                this.PreMove() &&
                this.MoveToTable() &&
                this.PostMove());
    }
}
