using System;
using HarmonyLib;
using Microsoft.Xna.Framework;
using MyCafe.Locations;
using Netcode;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.Network;
using StardewValley.Objects;
using StardewValley.Pathfinding;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using MyCafe.CustomerFactory;
using MyCafe.Customers;
using StardewValley.Inventories;
using StardewValley.ItemTypeDefinitions;
using StardewValley.Locations;
using xTile.Layers;
using xTile.Tiles;

namespace MyCafe;

[XmlType("Mods_MonsoonSheep_MyCafe_Cafe")]
public class Cafe : INetObject<NetFields>
{
    public NetFields NetFields { get; } = new NetFields("Cafe");

    internal readonly NetInt OpeningTime = new NetInt(630);
    internal readonly NetInt ClosingTime = new NetInt(2200);

    private readonly NetCollection<Table> _tables = [];
    private readonly NetBool _cafeEnabled = new NetBool();
    private readonly NetLocationRef _cafeIndoor = new NetLocationRef();
    private readonly NetLocationRef _cafeOutdoor = new NetLocationRef();

    internal int LastTimeCustomersArrived = 0;

    internal CustomerManager Customers;

    internal Dictionary<string, List<Item>> MenuItems = new Dictionary<string, List<Item>>();
    internal readonly List<Item> Recipes = new();

    internal bool Enabled
    {
        get => _cafeEnabled.Value;
        set => _cafeEnabled.Set(value);
    }

    internal CafeLocation Indoor
    {
        get => _cafeIndoor.Value as CafeLocation;
        set => _cafeIndoor.Set(value);
    }

    internal GameLocation Outdoor
    {
        get => _cafeOutdoor.Value;
        set => _cafeOutdoor.Set(value);
    }

    internal IList<Table> Tables
        => _tables as IList<Table>;

    public Cafe()
    {
        NetFields.SetOwner(this)
            .AddField(OpeningTime).AddField(ClosingTime).AddField(_tables).AddField(_cafeEnabled).AddField(_cafeIndoor.NetFields).AddField(_cafeOutdoor.NetFields);
    }

    internal void Initialize(IModHelper helper)
    {
        Customers = new CustomerManager(helper);
        _tables.OnValueAdded += delegate(Table table)
        {
            table.State.fieldChangeVisibleEvent += (_, oldValue, newValue) => OnTableStateChange(table, new TableStateChangedEventArgs()
            {
                OldValue = oldValue,
                NewValue = newValue
            });
        };

        var data = DataLoader.CookingRecipes(Game1.content);
        foreach (var key in SUtility.GetAllPlayerUnlockedCookingRecipes())
        {
            if (data.TryGetValue(key, out var value))
            {
                Item item = ItemRegistry.Create($"(O){ArgUtility.Get(value.Split('/'), 2)}");
                Recipes.Add(item);
            }
        }

        // DEBUG
        Customers.BusCustomers.State = SpawnerState.Enabled;
        Debug.SetMenuItems();
    }

    internal void DayUpdate()
    {
        Customers.DayUpdate();
    }

    internal void PopulateTables()
    {
        int count = 0;
        var locations = new List<GameLocation>();

        if (Indoor != null)
            locations.Add(Indoor);
        if (Outdoor != null)
            locations.Add(Outdoor);

        foreach (var location in locations)
        {
            foreach (Furniture furniture in location.furniture.Where(t => Utility.IsTable((t))))
            {
                // If we already have this table object registered, skip
                if (!Tables.OfType<FurnitureTable>().Any(t => t.ActualTable.Value.Equals(furniture)))
                {
                    FurnitureTable newTable = new FurnitureTable(furniture, location.Name);
                    if (TryAddTable(newTable))
                        count++;
                }
            }
        }

        if (count > 0)
        {
            Log.Debug($"{count} new furniture tables found in cafe locations.");
            count = 0;
        }

        // Remove duplicate tables
        for (var i = Tables.Count - 1; i >= 0; i--)
        {
            GameLocation location = Utility.GetLocationFromName(Tables[i].CurrentLocation);
            if (Tables[i] is FurnitureTable t && !location.furniture.Any(f => f.TileLocation == t.Position) ||
                locations.Any(l => Tables[i].CurrentLocation == l.Name))
            {
                Tables.RemoveAt(i);
            }
        }

        // Populate Map tables for cafe indoors
        if (Indoor != null)
        {
            Indoor.PopulateMapTables();
            foreach (var pair in Indoor.GetMapTables())
            {
                Rectangle newRectangle = new Rectangle(pair.Key.X * 64, pair.Key.Y * 64, pair.Key.Width * 64, pair.Key.Height * 64);
                LocationTable locationTable = new LocationTable(newRectangle, Indoor.Name, pair.Value);
                if (TryAddTable(locationTable))
                    count++;
                else
                {
                    Log.Error("Couldn't add location-based table");
                }
            }
            Log.Debug($"{count} new map-based tables found in cafe locations.");
        }
    }

    internal bool TryAddTable(Table table)
    {
        if (table.Seats.Count == 0)
            return false;

        table.Free();
        Tables.Add(table);
        return true;
    }

    internal void RemoveTable(FurnitureTable table)
    {
        if (!Tables.Contains(table))
            Log.Warn("Trying to remove a table that isn't tracked");
        else
        {
            Log.Debug($"Table removed");
            Tables.Remove(table);
        }
    }

    internal bool InteractWithTable(Table table, Farmer who)
    {
        switch (table.State.Value)
        {
            case TableState.CustomersDecidedOnOrder:
                table.State.Set(TableState.CustomersWaitingForFood);
                return true;
            case TableState.CustomersWaitingForFood:
            {
                var itemsNeeded = table.Seats.Where(s => s.ReservingCustomer != null).Select(c => c.ReservingCustomer.ItemToOrder.Value.ItemId).ToList();
                foreach (var item in itemsNeeded)
                {
                    if (!who.Items.ContainsId(item, minimum: itemsNeeded.Count(x => x == item)))
                    {
                        return false;
                    }
                }

                foreach (var item in itemsNeeded)
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

    internal void OnTableStateChange(object sender, TableStateChangedEventArgs e)
    {
        Table table = (Table) sender;

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
                foreach (Customer c in table.Seats.Where(s => s.ReservingCustomer != null).Select(s => s.ReservingCustomer))
                {
                    c.DrawItemOrder.Set(true);
                }
                Log.Debug("Table waiting for order");
                break;
            case TableState.CustomersEating:
                foreach (Customer c in table.Seats.Where(s => s.ReservingCustomer != null).Select(s => s.ReservingCustomer))
                {
                    c.DrawItemOrder.Set(false);
                }
                Log.Debug("Table eating");
                Game1.delayedActions.Add(new DelayedAction(2000, delegate ()
                {
                    table.State.Set(TableState.CustomersFinishedEating);
                })); 
                break;
            case TableState.CustomersFinishedEating:
                Log.Debug("Table finished meal");
                CustomerGroup group = Customers.GetGroupFromTable(table);
                Customers.LetGo(group);
                break;
        }
    }

    internal Table GetTableOfSeat(Seat seat)
    {
        Log.Error("Should remove this method");
        return Tables.FirstOrDefault(t => t.Seats.Contains(seat));
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
                    Indoor = indoor;
                    Outdoor = loc;
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
                        Outdoor = loc;
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
        MethodInfo method = AccessTools.Method(typeof(WarpPathfindingCache), "AddRoute", [typeof(List<string>), typeof(Gender?)]);
        if (method == null)
        {
            Log.Error("Couldn't find method to add route");
            return;
        }

        foreach (var location in Game1.locations)
        {
            List<string> route = WarpPathfindingCache.GetLocationRoute(location.Name, Outdoor.Name, Gender.Undefined)?.ToList();

            if (route == null && Outdoor.Equals(Game1.getFarm()))
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

            method.Invoke(null, [route, (object)null]);
            method.Invoke(null, [reverseRoute, (object)null]);

            if (Indoor != null)
            {
                route.Add(Indoor.Name);
                reverseRoute.Insert(0, Indoor.Name);

                method.Invoke(null, [route, (object)null]);
                method.Invoke(null, [reverseRoute, (object)null]);
            }
        }
    }

    public bool AddToMenu(Item itemToAdd, string category)
    {
        if (!MenuItems.ContainsKey(category) || MenuItems.Keys.Any(cat => MenuItems[cat].Any(x => x.ItemId == itemToAdd.ItemId)))
            return false;
        
        MenuItems[category].Add(itemToAdd);
        return true;
    }

    public void RemoveFromMenu(string category, string itemId)
    {
        MenuItems[category] = MenuItems[category].Where(x => x.ItemId != itemId).ToList();
        if (MenuItems[category].Count == 0)
            MenuItems.Remove(category);
    }
}

