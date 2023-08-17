using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using FarmCafe.Framework.Characters;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Objects;
using xTile.Dimensions;
using Microsoft.Xna.Framework;
using Rectangle = Microsoft.Xna.Framework.Rectangle;
using Utility = FarmCafe.Framework.Utilities.Utility;
using Vector2 = Microsoft.Xna.Framework.Vector2;


namespace FarmCafe.Framework.Objects
{
    public interface ITable
    {
        public List<ISeat> Seats { get; }

        public Vector2 Position { get; }

        public GameLocation CurrentLocation { get; }

        public bool IsReserved { get; }

        public Rectangle BoundingBox { get; }

        public void Free();

        public bool Reserve(List<Customer> customers);

        public Vector2 GetCenter();
    }

    internal class FurnitureTable : ITable
    {
        internal Furniture ActualTable;

        public FurnitureTable(Furniture actualTable, GameLocation location)
        {
            this.Seats = new List<ISeat>();
            this.ActualTable = actualTable;
            this.CurrentLocation = location;

            PopulateChairs();
        }

        public Vector2 Position => ActualTable.TileLocation;

        public List<ISeat> Seats { get; private set; }

        public GameLocation CurrentLocation { get; set; }

        public bool IsReserved => this.ActualTable.modData.TryGetValue("FarmCafeTableIsReserved", out var val) && val == "T";

        public Rectangle BoundingBox => this.ActualTable.boundingBox.Value;

        public void Free()
        {
            this.ActualTable.modData["FarmCafeTableIsReserved"] = "F";
            Seats.ForEach(s => s.Free());
        }

        public void PopulateChairs()
        {
            int sizeY = this.ActualTable.getTilesHigh();
            int sizeX = this.ActualTable.getTilesWide();

            for (int i = - 1; i <= sizeX ; i++)
            {
                for (int j = - 1; j <= sizeY; j++)
                {
                    if (
                            ( 
                            (i == -1 || i == sizeX) && (j == -1 || j == sizeY)
                            ) ||  // corners
                            ( 
                            !(i == -1 || i == sizeX) && !(j == -1 || j == sizeY)
                            )   // inside
                        )
                    {
                        continue;
                    }

                    Furniture chairAt = CurrentLocation.GetFurnitureAt(new Vector2(Position.X + i, Position.Y + j));
                    if (chairAt == null || !Utility.IsChair(chairAt)) 
                        continue;

                    int rotation = chairAt.currentRotation.Value;
                    bool valid = false;
                    valid = rotation switch
                    {
                        0 => (j == -1),
                        1 => (i == -1),
                        2 => (j == sizeY),
                        3 => (i == sizeX),
                        _ => valid
                    };

                    if (valid)
                    {
                        AddChair(chairAt);
                    }
                }
            }
        }

        public bool Reserve(List<Customer> customers)
        {
            if (IsReserved || Seats.Count < customers.Count)
                return false;

            this.ActualTable.modData["FarmCafeTableIsReserved"] = "T";
            for (int i = 0; i < customers.Count; i++)
            {
                customers[i].Seat = Seats[i];
                Seats[i].Reserve(customers[i]);
            }

            return true;
        }

        public Vector2 GetCenter()
        {
            return this.ActualTable.boundingBox.Value.Center.ToVector2();
        }

        internal FurnitureChair AddChair(Furniture chairToAdd)
        {
            if (IsReserved)
                return null;

            if (Seats.Any(c => c.TileLocation == chairToAdd.TileLocation))
                return null;

            Debug.Log("Adding chair to table");
            var furnitureChair = new FurnitureChair(chairToAdd)
            {
                Table = this
            };
            this.Seats.Add(furnitureChair);
            return furnitureChair;
        }

        internal bool RemoveChair(Furniture chairToRemove)
        {
            if (IsReserved)
                return false;

            if (!Seats.Any(c => c.TileLocation == chairToRemove.TileLocation))
            {
                Debug.Log("Trying to remove a chair that wasn't tracked");
                return false;
            }

            chairToRemove.modData.Remove("FarmCafeChairIsReserved");
            Seats = Seats.TakeWhile(c => c.TileLocation != chairToRemove.TileLocation).ToList();
            return true;
        }
    }

    public class MapTable : ITable {
        private bool isReserved;

        public MapTable(Rectangle boundingBox, GameLocation location, List<Vector2> seats)
        {
            this.BoundingBox = boundingBox;
            this.Position = boundingBox.Center.ToVector2();
            this.CurrentLocation = location;
            this.Seats = new List<ISeat>();

            foreach (var seat in seats)
            {
                MapChair mapSeat = new MapChair(this, seat);
                this.Seats.Add(mapSeat);
            }
        }

        public List<ISeat> Seats { get; }

        public Vector2 Position { get; }

        public GameLocation CurrentLocation { get; }

        public bool IsReserved => isReserved;

        public Rectangle BoundingBox { get; }

        public void Free()
        {
            isReserved = false;
            Seats.ForEach(s => s.Free());
        }

        public bool Reserve(List<Customer> customers)
        {
            if (isReserved)
                return false;
            
            for (int i = 0; i < customers.Count; i++)
            {
                customers[i].Seat = Seats[i];
                Seats[i].Reserve(customers[i]);
            }

            isReserved = true;
            return true;
        }

        public Vector2 GetCenter()
        {
            return BoundingBox.Center.ToVector2();
        }
    }
}
