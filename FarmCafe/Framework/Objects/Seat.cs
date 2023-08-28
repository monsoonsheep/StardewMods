using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FarmCafe.Framework.Characters;
using FarmCafe.Framework.Locations;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Objects;
using xTile.Tiles;

namespace FarmCafe.Framework.Objects
{
    internal abstract class Seat
    {
        internal virtual Table Table { get; set;  }

        internal virtual Vector2 Position { get; set; }

        internal virtual Customer ReservingCustomer { get; set; }

        internal virtual bool Reserve(Customer customer)
        {
            if (ReservingCustomer != null)
                return false;

            ReservingCustomer = customer;
            return true;
        }

        internal virtual void Free() 
            => ReservingCustomer = null;

        internal virtual bool IsReserved 
            => (ReservingCustomer != null);

        internal abstract int SittingDirection { get; }
    }

    internal sealed class FurnitureChair : Seat
    {
        internal Furniture ActualChair;

        public FurnitureChair(Furniture actualChair) 
            => this.ActualChair = actualChair;
        
        internal override Vector2 Position 
            => this.ActualChair.TileLocation;

        internal override int SittingDirection 
            => this.ActualChair.GetSittingDirection();

        internal override bool Reserve(Customer customer)
        {
            if (!base.Reserve(customer)) 
                return false;

            this.ActualChair.modData["FarmCafeChairIsReserved"] = "T";
            return true;
        }

        internal override void Free()
        {
            base.Free();
            this.ActualChair.modData.Remove("FarmCafeChairIsReserved");
        }
    }

    internal sealed class MapChair : Seat
    {
        public MapChair(Vector2 position) 
            => this.Position = position;
        
        internal override bool Reserve(Customer customer)
        {
            if (!base.Reserve(customer))
                return false;

            if (Table.CurrentLocation is CafeLocation cafe)
            {
                MapSeat mapSeat = cafe.mapSeats.FirstOrDefault(s => s.tilePosition.Value.Equals(this.Position));
                mapSeat?.sittingFarmers.Add(Game1.MasterPlayer.UniqueMultiplayerID, 0);
            }
            return true;
        }

        internal override void Free()
        {
            base.Free();
            if (Table.CurrentLocation is CafeLocation cafe)
            {
                MapSeat mapSeat = cafe.mapSeats.ToList().FirstOrDefault(s => s.tilePosition.Value.Equals(this.Position));
                mapSeat?.RemoveSittingFarmer(Game1.MasterPlayer);
            }
        }

        internal override int SittingDirection
        {
            get 
            {
                Rectangle tableBox = (Table as MapTable).BoundingBox;
                Vector2 myPos = Position * 64;
                if (tableBox.Contains(myPos.X, myPos.Y - 64))
                    return 0;
                if (tableBox.Contains(myPos.X + 64, myPos.Y))
                    return 1;
                if (tableBox.Contains(myPos.X, myPos.Y + 64))
                    return 2;
                if (tableBox.Contains(myPos.X - 64, myPos.Y))
                    return 3;

                return 0;
            }
        }
    }
}
