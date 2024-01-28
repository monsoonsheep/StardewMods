using Netcode;
using StardewValley.Objects;
using System.Xml.Serialization;

namespace MyCafe.Locations.Objects;

[XmlType("Mods_MonsoonSheep_MyCafe_FurnitureSeat")]
public sealed class FurnitureSeat : Seat
{
    internal readonly NetRef<Furniture> ActualChair = [];

    public FurnitureSeat() : base()
    {

    }

    public FurnitureSeat(Furniture actualChair, Table table) : base(table)
    {
        ActualChair.Set(actualChair);
        Position = ActualChair.Value.TileLocation.ToPoint();
    }

    protected override void InitNetFields()
    {
        base.InitNetFields();
        NetFields.AddField(ActualChair);
    }

    internal override int SittingDirection
        => ActualChair.Value?.GetSittingDirection() ?? 0;
}
