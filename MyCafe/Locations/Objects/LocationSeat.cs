using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using MonsoonSheep.Stardew.Common;
using Netcode;
using StardewValley;

namespace MyCafe.Locations.Objects;

public sealed class LocationSeat : Seat
{
    private readonly NetRef<MapSeat?> NetMapSeat = [null];

    public LocationSeat() : base()
    {
    }

    public LocationSeat(Point tilePosition, Table table) : base(table)
    {
        this.TilePosition = tilePosition;
        MapSeat? mapSeat = this.Location.mapSeats.FirstOrDefault(m => m.GetSeatBounds().Contains(new Vector2(this.TilePosition.X, this.TilePosition.Y)));
        if (mapSeat != null)
        {
            this.NetMapSeat.Set(mapSeat);
        }
        else
        {
            Log.Error("Couldn't set MapSeat for LocationSeat");
        }
    }

    protected override void InitNetFields()
    {
        base.InitNetFields();
        this.NetFields.AddField(this.NetMapSeat);
    }

    public override Vector2 SittingPosition
    {
        get
        {
            if (this.NetMapSeat.Value == null)
            {
                return this.TilePosition.ToVector2() * 64f;
            }
            else
            {
                return this.NetMapSeat.Value.GetSeatPositions()[0] * 64f;
            }
        }
    }

    internal override int SittingDirection
    {
        get
        {
            if (this.Table is LocationTable table)
            {
                Rectangle tableBox = table.BoundingBox.Value;
                Vector2 myPos = this.TilePosition.ToVector2() * 64f;
                if (tableBox.Contains(myPos.X, myPos.Y - 64))
                    return 0;
                if (tableBox.Contains(myPos.X + 64, myPos.Y))
                    return 1;
                if (tableBox.Contains(myPos.X, myPos.Y + 64))
                    return 2;
                if (tableBox.Contains(myPos.X - 64, myPos.Y))
                    return 3;
            }

            return 0;
        }
    }
}
