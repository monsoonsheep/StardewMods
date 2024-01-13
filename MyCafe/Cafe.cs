using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MyCafe.ChairsAndTables;
using Netcode;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.Network;
using StardewValley.Objects;
using StardewValley.Pathfinding;
using xTile.Layers;
using xTile.Tiles;
using SUtility = StardewValley.Utility;

namespace MyCafe;
[XmlType("Mods_MonsoonSheep_MyCafe_Cafe")]
public class Cafe : INetObject<NetFields>
{
    public NetFields NetFields { get; } = new NetFields("Cafe");

    internal NetInt OpeningTime = new NetInt(630);
    internal NetInt ClosingTime = new NetInt(2200);
    private readonly NetCollection<Table> _tables = new NetCollection<Table>();
    private readonly NetBool _cafeEnabled = new NetBool(false);
    private readonly NetLocationRef _cafeIndoor = new NetLocationRef();
    private readonly NetLocationRef _cafeOutdoor = new NetLocationRef(null);

    internal readonly IList<Item> MenuItems = new List<Item>(new Item[27]);
    internal readonly IList<Item> RecentlyAddedMenuItems = new List<Item>(new Item[9]);

    internal CustomerManager Customers;
    internal AssetManager Assets;

    internal bool Enabled
    {
        get => _cafeEnabled.Value;
        set => _cafeEnabled.Set(value);
    }

    internal GameLocation Indoor
    {
        get => _cafeIndoor.Value;
        set => _cafeIndoor.Set(value);
    }

    internal GameLocation Outdoor
    {
        get => _cafeOutdoor.Value;
        set => _cafeOutdoor.Set(value);
    }

    internal IList<Table> Tables 
        => _tables as IList<Table>;

    internal int LastTimeCustomersArrived = 0;

    internal readonly Dictionary<Rectangle, List<Vector2>> MapTablesInCafeLocation = new();

    public Cafe()
    {
        NetFields.SetOwner(this)
            .AddField(OpeningTime).AddField(ClosingTime).AddField(_tables).AddField(_cafeEnabled).AddField(_cafeIndoor.NetFields).AddField(_cafeOutdoor.NetFields);
    }

    internal void Initialize(IModHelper helper)
    {
        Assets = new AssetManager();
        Customers = new CustomerManager(helper);
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
            if (Tables[i] is FurnitureTable && !location.furniture.Any(f => f.TileLocation == Tables[i].Position) ||
                locations.Any(l => Tables[i].CurrentLocation == l.Name))
            {
                Tables.RemoveAt(i);
            }
        }

        // Populate Map tables for cafe indoors
        if (Indoor != null)
        {
            PopulateMapTables(Indoor);
            foreach (var pair in MapTablesInCafeLocation)
            {
                Rectangle newRectangle = new Rectangle(pair.Key.X * 64, pair.Key.Y * 64, pair.Key.Width * 64, pair.Key.Height * 64);
                LocationTable locationTable = new LocationTable(newRectangle, Indoor.Name, pair.Value);
                if (TryAddTable(locationTable))
                    count++;
            }
            Log.Debug($"{count} new map-based tables found in cafe locations.");
        }

        FreeAllTables();
    }

    internal void PopulateMapTables(GameLocation indoors)
    {
        if (MapTablesInCafeLocation is { Count: > 0 })
            return;

        MapTablesInCafeLocation.Clear();
        Layer layer = indoors.Map.GetLayer("Buildings");

        Dictionary<string, Rectangle> seatStringToTableRecs = new();

        for (int i = 0; i < layer.LayerWidth; i++)
        {
            for (int j = 0; j < layer.LayerHeight; j++)
            {
                Tile tile = layer.Tiles[i, j];
                if (tile == null)
                    continue;

                if (tile.TileIndexProperties.TryGetValue(ModKeys.MAPSEATS_TILEPROPERTY, out var val)
                    || tile.Properties.TryGetValue(ModKeys.MAPSEATS_TILEPROPERTY, out val))
                {
                    Rectangle thisTile = new Rectangle(i, j, 1, 1);

                    seatStringToTableRecs[val] = seatStringToTableRecs.TryGetValue(val, out var existingTileKey)
                        ? Rectangle.Union(thisTile, existingTileKey)
                        : thisTile;
                }
            }
        }

        foreach (var pair in seatStringToTableRecs)
        {
            var splitValues = pair.Key.Split(' ');
            var seats = new List<Vector2>();

            for (int i = 0; i < splitValues.Length; i += 2)
            {
                if (i + 1 >= splitValues.Length ||
                    !float.TryParse(splitValues[i], out float x) ||
                    !float.TryParse(splitValues[i + 1], out float y))
                {
                    Log.Debug($"Invalid values in Cafe Map's seats at {pair.Value.X}, {pair.Value.Y}", LogLevel.Warn);
                    return;
                }

                Vector2 seatLocation = new(x, y);
                seats.Add(seatLocation);
            }

            if (seats.Count > 0)
            {
                MapTablesInCafeLocation.Add(pair.Value, seats);
            }
        }

        Log.Debug($"Updated map tables in the cafe: {string.Join(", ", MapTablesInCafeLocation.Select(pair => pair.Key.Center + " with " + pair.Value.Count + " seats"))}");
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

    internal bool ChairIsReserved(Furniture chair)
    {
        return Tables.Any(t => t.Seats.OfType<FurnitureSeat>().Any(s => s.ActualChair.Value.Equals(chair)));
    }

    internal void FreeAllTables()
    {
        foreach (var table in Tables)
        {
            table.Free();
        }
    }

    internal Table GetTableAt(GameLocation location, Vector2 position)
    {
        return Tables.Where(t => t.CurrentLocation.Equals(location.Name)).FirstOrDefault(table => table.BoundingBox.Value.Contains(position));
    }

    internal void FarmerClickTable(Table table, Farmer who)
    {
        if (table.State.Value == TableState.CustomersDecidedOnOrder)
        {
            table.State.Set(TableState.CustomersWaitingForFood);
        }
    }

    internal Table GetTableOfSeat(Seat seat)
    {
        return Tables.FirstOrDefault(t => t.Seats.Contains(seat));
    }

    internal bool UpdateCafeLocations()
    {
        bool foundIndoor = false, foundSignboard = false;
        SUtility.ForEachLocation(delegate (GameLocation loc)
        {
            foreach (Building b in loc.buildings)
            {
                if (b.GetIndoors() is { Name: ModKeys.CAFE_BUILDING_BUILDING_ID } indoor)
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
        MethodInfo method = AccessTools.Method(typeof(WarpPathfindingCache), "AddRoute", new[] { typeof(List<string>), typeof(Gender?) });
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

            method.Invoke(null, new[] { route, (object)null });
            method.Invoke(null, new[] { reverseRoute, (object)null });

            if (Indoor != null)
            {
                route.Add(Indoor.Name);
                reverseRoute.Insert(0, Indoor.Name);

                method.Invoke(null, new[] { route, null });
                method.Invoke(null, new[] { reverseRoute, null });
            }
        }
    }

    
    internal bool OpenCafeMenuTileAction(GameLocation location, string[] args, Farmer player, Point tile)
    {
        if (!Context.IsMainPlayer)
            return false;

        if (Game1.activeClickableMenu == null && Context.IsPlayerFree)
        {
            Log.Debug("Opened cafe menu menu!");
            // Game1.activeClickableMenu = new CafeMenu();
        }

        return true;
    }

    public bool AddToMenu(Item itemToAdd)
    {
        if (MenuItems.Any(x => x.ItemId == itemToAdd.ItemId))
            return false;

        for (int i = 0; i < MenuItems.Count; i++)
        {
            if (MenuItems[i] == null)
            {
                MenuItems[i] = itemToAdd.getOne();
                MenuItems[i].Stack = 1;
                return true;
            }
        }

        return false;
    }

    public Item RemoveFromMenu(int slotNumber)
    {
        Item tmp = MenuItems[slotNumber];
        if (tmp == null)
            return null;

        MenuItems[slotNumber] = null;
        int firstEmpty = slotNumber;
        for (int i = slotNumber + 1; i < MenuItems.Count; i++)
        {
            if (MenuItems[i] != null)
            {
                MenuItems[firstEmpty] = MenuItems[i];
                MenuItems[i] = null;
                firstEmpty += 1;
            }
        }

        return tmp;
    }
}

public static class CafeSyncExtensions
{
    public static NetRef<Cafe> get_Cafe(this Farm farm)
    {
        return Mod.NetCafe;
    }

    public static void set_Cafe(this Farm farm, NetRef<Cafe> value)
    {
        //Mod.Cafe = value;
    }
}
