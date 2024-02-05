using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonsoonSheep.Stardew.Common;
using MyCafe.Characters.Spawning;
using MyCafe.Characters;
using MyCafe.Enums;
using MyCafe.Locations.Objects;
using Netcode;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.Inventories;
using StardewValley.Locations;
using StardewValley.Network;
using StardewValley.Objects;
using StardewValley.Pathfinding;
using SUtility = StardewValley.Utility;
using MyCafe.Inventories;
using MyCafe.Data.Customers;

namespace MyCafe;

public class Cafe : INetObject<NetFields>
{
    public NetFields NetFields { get; } = new NetFields("Cafe");

    internal CustomerManager Customers = null!;

    private readonly NetCollection<Table> NetTables = [];
    private readonly NetBool CafeEnabled = [];
    private readonly NetLocationRef CafeIndoor = new();
    private readonly NetLocationRef CafeOutdoor = new();

    public readonly NetInt OpeningTime = new(630);
    public readonly NetInt ClosingTime = new(2200);
    public readonly NetRef<MenuInventory> NetMenu = new(new MenuInventory());

    internal MenuInventory Menu => this.NetMenu.Value;

    internal int LastTimeCustomersArrived = 0;

    internal bool Enabled
    {
        get => this.CafeEnabled.Value;
        set => this.CafeEnabled.Set(value);
    }

    internal CafeLocation? Indoor
    {
        get => this.CafeIndoor.Value as CafeLocation;
        set => this.CafeIndoor.Set(value);
    }

    internal GameLocation? Outdoor
    {
        get => this.CafeOutdoor.Value;
        set => this.CafeOutdoor.Set(value);
    }

    internal IList<Table> Tables
        => this.NetTables as IList<Table>;

    public Cafe()
    {
        this.NetFields.SetOwner(this)
            .AddField(this.OpeningTime).AddField(this.ClosingTime).AddField(this.NetTables).AddField(this.CafeEnabled).AddField(this.CafeIndoor.NetFields)
            .AddField(this.CafeOutdoor.NetFields).AddField(this.NetMenu);
    }

    internal void Initialize(IModHelper helper, Dictionary<string, BusCustomerData> customersData)
    {
        this.Customers = new CustomerManager(helper, customersData, this.Tables);
        this.NetTables.OnValueAdded += delegate (Table table)
        {
            table.State.fieldChangeVisibleEvent += (_, oldValue, newValue) => this.OnTableStateChange(table, new TableStateChangedEventArgs()
            {
                OldValue = oldValue,
                NewValue = newValue
            });
        };

        // DEBUG
        this.Customers.BusCustomers.State = SpawnerState.Enabled;
    }

    internal void DayUpdate()
    {
        if (this.UpdateCafeLocations() is true)
        {
            this.Enabled = true;
            this.PopulateRoutesToCafe();
            this.PopulateTables();
            this.Customers.DayUpdate();
        }
        else
        {
            this.Enabled = false;
            this.Tables.Clear();
        }

        if (!this.Menu.Inventories.Any())
            this.Menu.AddCategory("Menu");
    }

    internal void PopulateTables()
    {
        this.Tables.Clear();
        int count = 0;
        var locations = new List<GameLocation>();

        if (this.Indoor != null)
            locations.Add(this.Indoor);
        if (this.Outdoor != null)
            locations.Add(this.Outdoor);

        foreach (var location in locations)
        {
            foreach (Furniture furniture in location.furniture.Where(t => Utility.IsTable((t))))
            {
                FurnitureTable newTable = new FurnitureTable(furniture, location.Name);
                if (newTable.Seats.Count > 0)
                {
                    this.TryAddTable(newTable);
                    count++;
                }
            }
        }

        if (count > 0)
        {
            Log.Debug($"{count} furniture tables found in cafe locations.");
            count = 0;
        }

        // Remove duplicate tables ?

        // Populate Map tables for cafe indoors
        if (this.Indoor != null)
        {
            this.Indoor.PopulateMapTables();
            foreach (var pair in this.Indoor.GetMapTables())
            {
                Rectangle newRectangle = new Rectangle(pair.Key.X * 64, pair.Key.Y * 64, pair.Key.Width * 64, pair.Key.Height * 64);
                LocationTable locationTable = new LocationTable(newRectangle, this.Indoor.Name, pair.Value);
                if (locationTable.Seats.Count > 0)
                {
                    this.TryAddTable(locationTable);
                    count++;
                }
            }
            Log.Debug($"{count} map-based tables found in cafe locations.");
        }
    }

    internal void TryAddTable(Table table)
    {
        table.Free();
        this.Tables.Add(table);
    }

    internal void RemoveTable(FurnitureTable table)
    {
        if (!this.Tables.Contains(table))
            Log.Warn("Trying to remove a table that isn't tracked");
        else
        {
            Log.Debug($"Table removed");
            this.Tables.Remove(table);
        }
    }

    internal static bool InteractWithTable(Table table, Farmer who)
    {
        switch (table.State.Value)
        {
            case TableState.CustomersDecidedOnOrder:
                table.State.Set(TableState.CustomersWaitingForFood);
                return true;
            case TableState.CustomersWaitingForFood:
                {
                    List<string?> itemsNeeded = table.Seats.Where(s => s.ReservingCustomer != null).Select(c => c.ReservingCustomer!.ItemToOrder.Value?.ItemId).ToList();
                    foreach (string? item in itemsNeeded)
                    {
                        if (!string.IsNullOrEmpty(item) && !who.Items.ContainsId(item, minimum: itemsNeeded.Count(x => x == item)))
                        {
                            return false;
                        }
                    }

                    foreach (string? item in itemsNeeded)
                    {
                        who.removeFirstOfThisItemFromInventory(item);
                    }

                    table.State.Set(TableState.CustomersEating);
                    return true;
                }
            default:
                return false;
        }
    }

    internal void OnFurniturePlaced(Furniture f, GameLocation location)
    {
        if (Utility.IsChair(f))
        {
            // Get position of table in front of the chair
            Vector2 tablePos = f.TileLocation + CommonHelper.DirectionIntToDirectionVector(f.currentRotation.Value) * new Vector2(1, -1);

            // Get table Furniture object
            Furniture facingFurniture = location.GetFurnitureAt(tablePos);

            if (facingFurniture == null ||
                !Utility.IsTable(facingFurniture) ||
                facingFurniture
                    .GetBoundingBox()
                    .Intersects(f.boundingBox.Value)) // if chair was placed on top of the table
                return;
            
            FurnitureTable table =
                this.TryGetFurnitureTable(facingFurniture, location, out FurnitureTable existing)
                    ? existing
                    : new FurnitureTable(facingFurniture, location.Name);

            table.AddChair(f);
            this.TryAddTable(table);
        }
        else if (Utility.IsTable(f))
        {
            if (!this.TryGetFurnitureTable(f, location, out _))
            {
                FurnitureTable table = new(f, location.Name);
                if (table.Seats.Count > 0)
                    this.TryAddTable(table);
            }
        }
    }

    internal bool TryGetFurnitureTable(Furniture table, GameLocation location, out FurnitureTable outTable)
    {
        FurnitureTable? t = Mod.Cafe.Tables
            .OfType<FurnitureTable>().FirstOrDefault(t => t.ActualTable.Value == table);

        if (t != null)
        {
            outTable = (FurnitureTable) t;
            return true;
        }
        
        outTable = null!;
        return false;
    }

    internal void OnFurnitureRemoved(Furniture f, GameLocation location)
    {
        if (Utility.IsChair(f))
        {
            FurnitureSeat? trackedChair = this.Tables
                .OfType<FurnitureTable>()
                .SelectMany(t => t.Seats)
                .OfType<FurnitureSeat>()
                .FirstOrDefault(seat => seat.ActualChair.Value.Equals(f));

            if (trackedChair?.Table is FurnitureTable table)
            {
                if (table.IsReserved)
                    Log.Warn("Removed a chair but the table was reserved");

                table.RemoveChair(f);
            }
        }
        else if (Utility.IsTable(f))
        {
            if (this.TryGetFurnitureTable(f, location, out FurnitureTable trackedTable))
            {
                this.RemoveTable(trackedTable);
            }
        }
    }

    internal void OnTableStateChange(object sender, TableStateChangedEventArgs e)
    {
        Table table = (Table)sender;

        if (e.OldValue == e.NewValue)
            return;

        switch (e.NewValue)
        {
            case TableState.CustomersThinkingOfOrder:
                Log.Debug("Table started");
                Game1.delayedActions.Add(new DelayedAction(2000, delegate ()
                {
                    table.State.Set(TableState.CustomersDecidedOnOrder);
                }));
                break;
            case TableState.CustomersDecidedOnOrder:
                Log.Debug("Table decided");
                break;
            case TableState.CustomersWaitingForFood:
                foreach (Customer? c in table.Seats.Select(s => s.ReservingCustomer))
                {
                    c?.DrawItemOrder.Set(true);
                }
                Log.Debug("Table waiting for order");
                break;
            case TableState.CustomersEating:
                foreach (Customer? c in table.Seats.Select(s => s.ReservingCustomer))
                {
                    c?.DrawItemOrder.Set(false);
                }
                Log.Debug("Table eating");
                Game1.delayedActions.Add(new DelayedAction(2000, delegate ()
                {
                    table.State.Set(TableState.CustomersFinishedEating);
                }));
                break;
            case TableState.CustomersFinishedEating:
                Log.Debug("Table finished meal");
                CustomerGroup? group = this.Customers.GetGroupFromTable(table);
                if (group != null)
                    this.Customers.LetGo(group);
                else
                    Log.Error("Problem getting group from table!");
                break;
        }
    }

    internal Table GetTableOfSeat(Seat seat)
    {
        Log.Error("Should remove this method");
        return this.Tables.FirstOrDefault(t => t.Seats.Contains(seat))!;
    }

    internal bool UpdateCafeLocations()
    {
        bool foundIndoor = false, foundSignboard = false;
        SUtility.ForEachLocation(delegate (GameLocation loc)
        {
            foreach (Building b in loc.buildings)
            {
                if (b.GetIndoors() is CafeLocation indoor)
                {
                    foundIndoor = true;
                    this.Indoor = indoor;
                    this.Outdoor = loc;
                    Game1._locationLookup.TryAdd(indoor.Name, indoor);
                    Game1._locationLookup.TryAdd(loc.Name, loc);
                    return false;
                }
            }

            return true;
        });

        if (!foundIndoor)
        {
            SUtility.ForEachLocation(delegate (GameLocation loc)
            {
                foreach (Building b in loc.buildings)
                {
                    if (b.GetData() is { DefaultAction: ModKeys.SIGNBOARD_BUILDING_CLICK_EVENT_KEY })
                    {
                        foundSignboard = true;
                        this.Outdoor = loc;
                        Game1._locationLookup.TryAdd(loc.Name, loc);
                        return false;
                    }
                }

                return true;
            });
        }

        return foundSignboard || foundIndoor;
    }


    internal void PopulateRoutesToCafe()
    {
        MethodInfo addRouteMethod = AccessTools.Method(typeof(WarpPathfindingCache), "AddRoute", [typeof(List<string>), typeof(Gender?)]);
        if (addRouteMethod == null || this.Outdoor == null)
        {
            Log.Error("Couldn't populate routes to cafe");
            return;
        }

        foreach (var location in Game1.locations)
        {
            List<string>? route = WarpPathfindingCache.GetLocationRoute(location.Name, this.Outdoor.Name, Gender.Undefined)?.ToList();

            if (route == null && this.Outdoor.Equals(Game1.getFarm()))
            {
                if (location.Name.Equals("BusStop"))
                {
                    route = ["BusStop"];
                }
                else
                {
                    route = WarpPathfindingCache.GetLocationRoute(location.Name, "BusStop", Gender.Undefined)?.ToList();
                }

                route?.Add("Farm");
            }

            if (route is not { Count: > 1 })
                continue;

            var reverseRoute = new List<string>(route);
            reverseRoute.Reverse();

            addRouteMethod.Invoke(null, [route, null]);
            addRouteMethod.Invoke(null, [reverseRoute, null]);

            if (this.Indoor != null)
            {
                route.Add(this.Indoor.Name);
                reverseRoute.Insert(0, this.Indoor.Name);

                addRouteMethod.Invoke(null, [route, null]);
                addRouteMethod.Invoke(null, [reverseRoute, null]);
            }
        }
    }
}

