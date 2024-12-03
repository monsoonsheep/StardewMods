using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Netcode;
using StardewMods.MyShops.Framework.Enums;
using StardewValley;


namespace StardewMods.MyShops.Framework.Objects;

public abstract class Table : INetObject<NetFields>
{
    public NetFields NetFields { get; }

    internal NetEnum<TableState> State = new NetEnum<TableState>(TableState.Free);

    private readonly NetString _netLocation = [];

    private readonly NetVector2 _netTilePosition = [];

    internal NetRectangle BoundingBox { get; set; } = [];

    internal readonly NetCollection<Seat> Seats = [];

    internal virtual Vector2 TilePosition
    {
        get => this._netTilePosition.Value;
        set => this._netTilePosition.Set(value);
    }

    internal string Location
    {
        get => this._netLocation.Value;
        set => this._netLocation.Set(value);
    }

    internal Vector2 Center => this.BoundingBox.Center.ToVector2();

    internal bool IsReserved => this.State.Value != TableState.Free;

    protected Table()
    {
        this.NetFields = new NetFields(NetFields.GetNameForInstance(this));
        this.NetFields.SetOwner(this)
            .AddField(this.State).AddField(this.Seats).AddField(this._netLocation).AddField(this._netTilePosition).AddField(this.BoundingBox);
    }

    protected Table(string location) : this()
    {
        this.Location = location;
    }

    internal virtual void Free()
    {
        // Log.Debug($"Freeing {(this is LocationTable ? "Location" : "Furniture")} table");
        this.State.Set(TableState.Free);
        foreach (Seat s in this.Seats)
            s.Free();
    }

    internal virtual bool Reserve(List<NPC> customers)
    {
        if (this.IsReserved || this.Seats.Count < customers.Count)
            return false;

        for (int i = 0; i < customers.Count; i++)
        {
            this.Seats[i].Reserve(customers[i]);
        }

        this.State.Set(TableState.CustomersComing);
        return true;
    }
}
