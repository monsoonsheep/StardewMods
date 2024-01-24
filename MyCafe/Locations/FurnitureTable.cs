using MyCafe.Customers;
using Netcode;
using StardewValley;
using StardewValley.Objects;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;


namespace MyCafe.Locations;

[XmlType("Mods_MonsoonSheep_MyCafe_FurnitureTable")]
public class FurnitureTable : Table
{
    internal readonly NetRef<Furniture> ActualTable = [];

    public FurnitureTable() : base()
    {

    }

    internal FurnitureTable(Furniture actualTable, string location) : base(location)
    {
        ActualTable.Set(actualTable);
        base.BoundingBox.Set(actualTable.boundingBox.Value);
        base.Position = ActualTable.Value.TileLocation;
        PopulateChairs();
    }

    protected override void InitNetFields()
    {
        base.InitNetFields();
        base.NetFields.AddField(ActualTable);
    }

    internal override bool Reserve(List<Customer> customers)
    {
        if (!base.Reserve(customers))
            return false;

        return true;
    }

    internal void PopulateChairs()
    {
        int sizeY = ActualTable.Value.getTilesHigh();
        int sizeX = ActualTable.Value.getTilesWide();

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

    internal FurnitureSeat AddChair(Furniture chairToAdd)
    {
        if (IsReserved)
            return null;

        if (Seats.Any(c => c.Position == chairToAdd.TileLocation.ToPoint()))
            return null;

        Log.Debug("Adding chair to table");
        var furnitureChair = new FurnitureSeat(chairToAdd, this);
        Seats.Add(furnitureChair);
        return furnitureChair;
    }

    internal bool RemoveChair(Furniture chairToRemove)
    {
        if (IsReserved)
            return false;

        if (!Seats.Any(c => c.Position == chairToRemove.TileLocation.ToPoint()))
        {
            Log.Debug("Trying to remove a chair that wasn't tracked");
            return false;
        }

        Seats.Set(Seats.TakeWhile(c => c.Position != chairToRemove.TileLocation.ToPoint()).ToList());
        Log.Debug("Removed chair from table");
        return true;
    }
}
