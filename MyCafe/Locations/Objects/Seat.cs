using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.Xna.Framework;
using MonsoonSheep.Stardew.Common;
using Monsoonsheep.StardewMods.MyCafe.Game;
using Netcode;
using StardewValley;
// ReSharper disable VirtualMemberCallInConstructor

namespace Monsoonsheep.StardewMods.MyCafe.Locations.Objects;

public abstract class Seat : INetObject<NetFields>
{
    public NetFields NetFields { get; }

    private readonly NetPoint _netTilePosition = [];

    private readonly NetRef<NPC?> _netReservingCustomer = [];

    internal Table Table
        => Mod.Cafe.Tables.FirstOrDefault(t => t.Seats.Contains(this))!;

    internal NPC? ReservingCustomer
    {
        get => this._netReservingCustomer.Value;
        set => this._netReservingCustomer.Set(value);
    }

    internal GameLocation Location
        => CommonHelper.GetLocation(this.Table.Location)!;

    internal Point TilePosition
    {
        get => this._netTilePosition.Value;
        set => this._netTilePosition.Set(value);
    }

    internal virtual int SittingDirection { get; set; }

    internal bool IsReserved
        => this.ReservingCustomer != null;

    internal abstract Vector2 SittingPosition { get; }

    protected Seat()
    {
        this.NetFields = new NetFields(NetFields.GetNameForInstance(this));
        this.NetFields.SetOwner(this)
            .AddField(this._netTilePosition).AddField(this._netReservingCustomer);
    }

    internal bool Reserve(NPC customer)
    {
        if (this.ReservingCustomer != null)
            return false;

        customer.set_Seat(this);
        this.ReservingCustomer = customer;
        return true;
    }

    internal void Free()
    {
        this.ReservingCustomer?.set_Seat(null);
        this.ReservingCustomer = null;
    }
}
