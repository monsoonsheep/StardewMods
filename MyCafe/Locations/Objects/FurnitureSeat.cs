using System.Xml.Serialization;
using Netcode;
using StardewValley.Objects;

namespace MyCafe.Locations.Objects;

public sealed class FurnitureSeat : Seat
{
    internal readonly NetRef<Furniture> ActualChair = [];

    public FurnitureSeat() : base()
    {

    }

    public FurnitureSeat(Furniture actualChair, Table table) : base(table)
    {
        this.ActualChair.Set(actualChair);
        this.Position = this.ActualChair.Value.TileLocation.ToPoint();
    }

    protected override void InitNetFields()
    {
        base.InitNetFields();
        this.NetFields.AddField(this.ActualChair);
    }

    internal override int SittingDirection
        =>
            this.ActualChair.Value?.GetSittingDirection() ?? 0;
}
