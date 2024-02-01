using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Xml.Serialization;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonsoonSheep.Stardew.Common;
using MyCafe.Customers.Spawning;
using MyCafe.Customers;
using MyCafe.Customers.Data;
using MyCafe.Enums;
using MyCafe.Locations.Objects;
using Netcode;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.Locations;
using StardewValley.Network;
using StardewValley.Objects;
using StardewValley.Pathfinding;
using SUtility = StardewValley.Utility;

namespace MyCafe;

[XmlType("Mods_MonsoonSheep_MyCafe_Cafe")]
public class Cafe : INetObject<NetFields>
{
    internal Texture2D Sprites = null!;

    internal CustomerManager Customers = null!;

    public NetFields NetFields { get; } = new NetFields("Cafe");

    internal readonly NetInt OpeningTime = new NetInt(630);
    internal readonly NetInt ClosingTime = new NetInt(2200);
    internal int LastTimeCustomersArrived = 0;

    private readonly NetCollection<Table> NetTables = [];
    private readonly NetBool CafeEnabled = new NetBool();
    private readonly NetLocationRef CafeIndoor = new NetLocationRef();
    private readonly NetLocationRef CafeOutdoor = new NetLocationRef();


    internal Dictionary<string, List<Item>> MenuItems = new Dictionary<string, List<Item>>();
    internal readonly List<Item> Recipes = new();

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
        =>
            this.NetTables as IList<Table>;

    public Cafe()
    {
        this.NetFields.SetOwner(this)
            .AddField(this.OpeningTime).AddField(this.ClosingTime).AddField(this.NetTables).AddField(this.CafeEnabled).AddField(this.CafeIndoor.NetFields).AddField(this.CafeOutdoor.NetFields);
    }

    internal void Initialize(IModHelper helper, Dictionary<string, BusCustomerData> customersData)
    {
        this.Customers = new CustomerManager(helper, customersData, this);
        this.NetTables.OnValueAdded += delegate (Table table)
        {
            table.State.fieldChangeVisibleEvent += (_, oldValue, newValue) => this.OnTableStateChange(table, new TableStateChangedEventArgs()
            {
                OldValue = oldValue,
                NewValue = newValue
            });
        };

        var data = DataLoader.CookingRecipes(Game1.content);
        foreach (string? key in SUtility.GetAllPlayerUnlockedCookingRecipes())
        {
            if (data.TryGetValue(key, out string? value))
            {
                Item item = ItemRegistry.Create($"(O){ArgUtility.Get(value.Split('/'), 2)}");
                this.Recipes.Add(item);
            }
        }

        // DEBUG
        this.Customers.BusCustomers.State = SpawnerState.Enabled;
        Debug.SetMenuItems();
    }

    internal void DayUpdate()
    {
        this.Customers.DayUpdate();
    }

    internal void PopulateTables()
    {
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
                // If we already have this table object registered, skip
                if (!this.Tables.OfType<FurnitureTable>().Any(t => t.ActualTable.Value.Equals(furniture)))
                {
                    FurnitureTable newTable = new FurnitureTable(furniture, location.Name);
                    if (newTable.Seats.Count > 0)
                    {
                        this.TryAddTable(newTable);
                        count++;
                    }
                }
            }
        }

        if (count > 0)
        {
            Log.Debug($"{count} new furniture tables found in cafe locations.");
            count = 0;
        }

        // Remove duplicate tables
        for (int i = this.Tables.Count - 1; i >= 0; i--)
        {
            GameLocation? location = CommonHelper.GetLocation(this.Tables[i].CurrentLocation);
            if (location != null
                && this.Tables[i] is FurnitureTable t
                && !location.furniture.Any(f => f.TileLocation == t.Position)
                || locations.Any(l => this.Tables[i].CurrentLocation == l.Name))
            {
                this.Tables.RemoveAt(i);
            }
        }

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
            Log.Debug($"{count} new map-based tables found in cafe locations.");
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

    internal bool InteractWithTable(Table table, Farmer who)
    {
        switch (table.State.Value)
        {
            case TableState.CustomersDecidedOnOrder:
                table.State.Set(TableState.CustomersWaitingForFood);
                return true;
            case TableState.CustomersWaitingForFood:
                {
                    var itemsNeeded = table.Seats.Where(s => s.ReservingCustomer != null).Select(c => c.ReservingCustomer!.ItemToOrder.Value?.ItemId).ToList();
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

    public bool AddToMenu(Item itemToAdd, string category, int index = 0)
    {
        if (!this.MenuItems.ContainsKey(category) || this.MenuItems.Keys.Any(cat => this.MenuItems[cat].Any(x => x.ItemId == itemToAdd.ItemId)))
            return false;

        if (index >= this.MenuItems[category].Count)
            this.MenuItems[category].Add(itemToAdd);
        else
            this.MenuItems[category].Insert(index, itemToAdd);

        return true;
    }

    public void RemoveFromMenu(Item item, string? category = null)
    {
        if (string.IsNullOrEmpty(category))
            category = this.MenuItems.Keys.FirstOrDefault(key => this.MenuItems[key].Contains(item));
        if (string.IsNullOrEmpty(category))
        {
            Log.Error("Couldn't find the item to remove");
            return;
        }

        this.MenuItems[category].Remove(item);
    }
}

