using System.Linq;
using Microsoft.Xna.Framework;
using MonsoonSheep.Stardew.Common;
using Netcode;
using StardewValley;
using StardewValley.Objects;


namespace MyCafe.Locations.Objects;

public class FurnitureTable : Table
{
    internal readonly NetRef<Furniture> ActualTable = [];

    public FurnitureTable() : base()
    {

    }

    internal FurnitureTable(Furniture actualTable) : base(actualTable.Location.Name)
    {
        this.ActualTable.Set(actualTable);
        base.BoundingBox.Set(actualTable.boundingBox.Value);
        base.Position = this.ActualTable.Value.TileLocation;
        this.PopulateChairs();
    }

    protected override void InitNetFields()
    {
        base.InitNetFields();
        this.NetFields.AddField(this.ActualTable);
    }

    internal void PopulateChairs()
    {
        int sizeY = this.ActualTable.Value.getTilesHigh();
        int sizeX = this.ActualTable.Value.getTilesWide();

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

                GameLocation? location = CommonHelper.GetLocation(this.CurrentLocation);
                Furniture? chairAt = location?.GetFurnitureAt(new Vector2(this.Position.X + i, this.Position.Y + j));
                if (chairAt == null || !ModUtility.IsChair(chairAt))
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
                    this.AddChair(chairAt);
                }
            }
        }
    }

    internal FurnitureSeat? AddChair(Furniture chairToAdd)
    {
        if (this.IsReserved)
            return null;

        if (this.Seats.Any(c => c.TilePosition == chairToAdd.TileLocation.ToPoint()))
            return null;

        Log.Debug("Adding chair to table");
        var furnitureChair = new FurnitureSeat(chairToAdd, this);
        this.Seats.Add(furnitureChair);
        return furnitureChair;
    }

    internal bool RemoveChair(Furniture chairToRemove)
    {
        if (this.IsReserved)
            return false;

        if (!this.Seats.Any(c => c.TilePosition == chairToRemove.TileLocation.ToPoint()))
        {
            Log.Debug("Trying to remove a chair that wasn't tracked");
            return false;
        }
        this.Seats.RemoveWhere(s => s.TilePosition.X == (int) chairToRemove.TileLocation.X && s.TilePosition.Y == (int) chairToRemove.TileLocation.Y);
        //this.Seats.Set(this.Seats.TakeWhile(c => c.Position != chairToRemove.TileLocation.ToPoint()).ToList());
        Log.Debug("Removed chair from table");
        return true;
    }
}
