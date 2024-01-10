using Microsoft.Xna.Framework;
using MyCafe.Customers;
using Netcode;
using StardewValley.Objects;
using System.Xml.Serialization;

namespace MyCafe.ChairsAndTables;

[XmlType("Mods_MonsoonSheep_MyCafe_FurnitureSeat")]
public sealed class FurnitureSeat : Seat
{
    internal readonly NetRef<Furniture> ActualChair = new NetRef<Furniture>();

    public FurnitureSeat() : base()
    {

    }

    public FurnitureSeat(Furniture actualChair, Table table) : base(table)
    {
        ActualChair.Set(actualChair);
        base.Position = ActualChair.Value.TileLocation.ToPoint();
    }

    protected override void InitNetFields()
    {
        base.InitNetFields();
        base.NetFields.AddField(ActualChair);
    }

    internal override int SittingDirection
        => ActualChair.Value?.GetSittingDirection() ?? 0;
}
