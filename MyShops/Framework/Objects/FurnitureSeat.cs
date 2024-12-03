using Microsoft.Xna.Framework;
using Netcode;
using StardewValley.Objects;

namespace StardewMods.MyShops.Framework.Objects;

public sealed class FurnitureSeat : Seat
{
    internal readonly NetRef<Furniture> ActualChair = [];

    public FurnitureSeat() : base()
    {
        this.NetFields.AddField(this.ActualChair);
    }

    public FurnitureSeat(Furniture actualChair) : this()
    {
        this.ActualChair.Set(actualChair);
        this.TilePosition = this.ActualChair.Value.TileLocation.ToPoint();
    }

    internal override Vector2 SittingPosition
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
