using System.Linq;
using Microsoft.Xna.Framework;
using MyCafe.Framework.Customers;
using StardewValley;
using StardewValley.Objects;

namespace MyCafe.Framework.Objects
{
    internal abstract class Seat
    {
        internal virtual Table Table { get; set; }

        internal virtual Vector2 Position { get; set; }

        internal virtual Customer ReservingCustomer { get; set; }

        internal abstract int SittingDirection { get; }

        internal virtual bool IsReserved => ReservingCustomer != null;

        internal virtual bool Reserve(Customer customer)
        {
            if (ReservingCustomer != null)
                return false;

            ReservingCustomer = customer;
            return true;
        }

        internal virtual void Free()
            => ReservingCustomer = null;

    }

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

    internal sealed class MapChair : Seat
    {
        public MapChair(Vector2 position)
            => Position = position;
        internal override int SittingDirection
        {
            get
            {
                if (Table is MapTable table)
                {
                    Rectangle tableBox = table.BoundingBox;
                    Vector2 myPos = Position * 64;
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

        internal override bool Reserve(Customer customer)
        {
            if (!base.Reserve(customer))
                return false;

            GameLocation location = Game1.getLocationFromName(Table.CurrentLocation);
            MapSeat mapSeat = location.mapSeats.FirstOrDefault(s => s.tilePosition.Value.Equals(Position));
            mapSeat?.sittingFarmers.Add(Game1.MasterPlayer.UniqueMultiplayerID, 0);
            
            return true;
        }

        internal override void Free()
        {
            base.Free();
            GameLocation location = Game1.getLocationFromName(Table.CurrentLocation);
            MapSeat mapSeat = location.mapSeats.ToList().FirstOrDefault(s => s.tilePosition.Value.Equals(Position));
            mapSeat?.RemoveSittingFarmer(Game1.MasterPlayer);
        
        }

    }
}
