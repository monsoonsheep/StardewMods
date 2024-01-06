using Microsoft.Xna.Framework;
using MyCafe.Framework.Customers;
using StardewValley.Objects;

namespace MyCafe.Framework.ChairsAndTables;

internal sealed class FurnitureChair : Seat
{
    internal Furniture ActualChair;

    public FurnitureChair(Furniture actualChair)
        => ActualChair = actualChair;

    internal override Vector2 Position
        => ActualChair.TileLocation;

    internal override int SittingDirection
        => ActualChair.GetSittingDirection();

    internal override bool Reserve(Customer customer)
    {
        if (!base.Reserve(customer))
            return false;

        ActualChair.modData["VisitorFrameworkChairIsReserved"] = "T";
        return true;
    }

    internal override void Free()
    {
        base.Free();
        ActualChair.modData.Remove(ModKeys.MODDATA_CHAIRRESERVED);
    }
}
