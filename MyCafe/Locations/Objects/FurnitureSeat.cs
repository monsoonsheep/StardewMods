using Microsoft.Xna.Framework;
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
        this.TilePosition = this.ActualChair.Value.TileLocation.ToPoint();
    }

    protected override void InitNetFields()
    {
        base.InitNetFields();
        this.NetFields.AddField(this.ActualChair);
    }

    public override Vector2 SittingPosition
    {
        get
        {
            Vector2 pos = this.ActualChair.Value.GetSeatPositions()[0] * 64f;
            switch (this.SittingDirection)
            {
                case 0:
                    pos.Y -= 12f;
                    break;
                case 1:
                    pos.X += 10f;
                    break;
                case 2:
                    pos.Y += 4f;
                    break;
                case 3:
                    pos.X -= 10f;
                    break;
            }

            return pos;
        }
    }

    internal override int SittingDirection
        =>
            this.ActualChair.Value?.GetSittingDirection() ?? 0;
}
