using System.Collections.Generic;
using System.Linq;
using MyCafe.Framework.Customers;
using StardewValley;
using StardewValley.Objects;
using Rectangle = Microsoft.Xna.Framework.Rectangle;
using Vector2 = Microsoft.Xna.Framework.Vector2;


namespace MyCafe.Framework.Objects
{
    internal abstract class Table
    {
        internal bool IsReadyToOrder;

        protected Table(string location)
        {
            CurrentLocation = location;
        }

        internal virtual List<Seat> Seats { get; set; }

        internal virtual Vector2 Position { get; set; }

        internal string CurrentLocation { get; set; }

        internal virtual bool IsReserved { get; set; }

        internal virtual Rectangle BoundingBox { get; set; }

        internal virtual void Free()
        {
            IsReadyToOrder = false;
            Seats.ForEach(s => s.Free());
            IsReserved = false;
        }

        internal virtual bool Reserve(List<Customer> customers)
        {
            if (IsReserved || Seats.Count < customers.Count)
                return false;

            for (int i = 0; i < customers.Count; i++)
            {
                customers[i].modData["MonsoonSheep.MyCafe_ModDataSeatPos"] = $"{Seats[i].Position.X} {Seats[i].Position.Y}";
                Seats[i].Reserve(customers[i]);
            }

            IsReserved = true;
            return true;
        }

        internal virtual Vector2 GetCenter()
            => BoundingBox.Center.ToVector2();
    }

    internal class FurnitureTable : Table
    {
        internal Furniture ActualTable;

        internal FurnitureTable(Furniture actualTable, string location) : base(location)
        {
            base.Seats = new List<Seat>();
            ActualTable = actualTable;

            PopulateChairs();
        }

        internal override Vector2 Position
            => ActualTable.TileLocation;

        internal override bool IsReserved
            => ActualTable.modData.TryGetValue(ModKeys.MODDATA_TABLERESERVED, out var val) && val == "T";

        internal override Rectangle BoundingBox
            => ActualTable.boundingBox.Value;

        internal override void Free()
        {
            base.Free();
            ActualTable.modData[ModKeys.MODDATA_TABLERESERVED] = "F";
        }

        internal override bool Reserve(List<Customer> customers)
        {
            if (!base.Reserve(customers))
                return false;

            ActualTable.modData[ModKeys.MODDATA_TABLERESERVED] = "T";
            return true;
        }

        internal void PopulateChairs()
        {
            int sizeY = ActualTable.getTilesHigh();
            int sizeX = ActualTable.getTilesWide();

            for (int i = -1; i <= sizeX; i++)
            {
                for (int j = -1; j <= sizeY; j++)
                {
                    if (
                            
                            (i == -1 || i == sizeX) && (j == -1 || j == sizeY)
                             ||  // corners
                            
                            !(i == -1 || i == sizeX) && !(j == -1 || j == sizeY)
                               // inside
                        )
                    {
                        continue;
                    }

                    GameLocation location = Utility.GetLocationFromName(CurrentLocation);
                    Furniture chairAt = location.GetFurnitureAt(new Vector2(Position.X + i, Position.Y + j));
                    if (chairAt == null || !Utility.IsChair(chairAt))
                        continue;

                    int rotation = chairAt.currentRotation.Value;
                    bool valid = rotation switch
                    {
                        0 => j == -1,
                        1 => i == -1,
                        2 => j == sizeY,
                        3 => i == sizeX,
                        _ => false
                    };

                    if (valid)
                    {
                        AddChair(chairAt);
                    }
                }
            }
        }

        internal FurnitureChair AddChair(Furniture chairToAdd)
        {
            if (IsReserved)
                return null;

            if (Seats.Any(c => c.Position == chairToAdd.TileLocation))
                return null;

            Log.Debug("Adding chair to table");
            var furnitureChair = new FurnitureChair(chairToAdd)
            {
                Table = this
            };
            Seats.Add(furnitureChair);
            return furnitureChair;
        }

        internal bool RemoveChair(Furniture chairToRemove)
        {
            if (IsReserved)
                return false;

            if (!Seats.Any(c => c.Position == chairToRemove.TileLocation))
            {
                Log.Debug("Trying to remove a chair that wasn't tracked");
                return false;
            }

            chairToRemove.modData.Remove(ModKeys.MODDATA_CHAIRRESERVED);
            Seats = Seats.TakeWhile(c => c.Position != chairToRemove.TileLocation).ToList();
            Log.Debug("Removed chair from table");
            return true;
        }
    }

    internal class MapTable : Table
    {
        internal MapTable(Rectangle boundingBox, string location, List<Vector2> seatPositions) : base(location)
        {
            base.BoundingBox = boundingBox;
            base.Position = boundingBox.Center.ToVector2();

            base.Seats = new List<Seat>();
            foreach (var seat in seatPositions)
            {
                var mapSeat = new MapChair(seat)
                {
                    Table = this
                };
                base.Seats.Add(mapSeat);
            }
        }
    }
}
