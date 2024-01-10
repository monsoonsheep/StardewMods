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

    public NetInt OpeningTime = new NetInt(630);
    public NetInt ClosingTime = new NetInt(2200);
    public NetCollection<Table> Tables = new NetCollection<Table>();

    public NetBool NetCafeEnabled = new NetBool(false);
    public NetString NetCafeIndoor = new NetString(null);
    public NetString NetCafeOutdoor = new NetString(null);

    internal bool Enabled
    {
        get => NetCafeEnabled.Value;
        set => NetCafeEnabled.Set(value);
    }

    internal GameLocation Indoor
    {
        get => NetCafeIndoor.Value != null ? Game1.getLocationFromName(NetCafeIndoor.Value) : null;
        set => NetCafeIndoor.Set(value.Name);
    }

    internal GameLocation Outdoor
    {
        get => NetCafeOutdoor.Value != null ? Game1.getLocationFromName(NetCafeOutdoor.Value) : null;
        set => NetCafeOutdoor.Set(value.Name);
    }

    internal int LastTimeCustomersArrived = 0;

    internal readonly Dictionary<Rectangle, List<Vector2>> MapTablesInCafeLocation = new();


    public Cafe()
    {
        InitNetFields();
    }

    private void InitNetFields()
    {
        NetFields.SetOwner(this)
            .AddField(OpeningTime).AddField(ClosingTime).AddField(Tables).AddField(NetCafeEnabled).AddField(NetCafeIndoor).AddField(NetCafeOutdoor);
    }

    internal void Initialize(IModHelper helper)
    {
        
    }

    internal void DayUpdate(object sender, DayStartedEventArgs e)
    {
        if (Enabled)
            return;

        if (UpdateCafeLocations() is false)
        {
            Enabled = false;
        }
        else
        {
            Enabled = true;
            if (Context.IsMainPlayer)
            {
                Mod.ModHelper.Events.World.FurnitureListChanged += OnFurnitureListChanged;
                PopulateTables();
                PopulateRoutesToCafe();
            }
        }
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

    internal FurnitureTable TryAddFurnitureTable(Furniture table, GameLocation location)
    {
        FurnitureTable newTable = new FurnitureTable(table, location.Name);

        return TryAddTable(newTable)
            ? newTable : null;
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

    }

    internal void OnRenderedWorld(object sender, RenderedWorldEventArgs e)
    {
        // get list of reserved tables with center coords
        foreach (var table in Tables)
        {

            if (table.IsReadyToOrder.Value && Game1.currentLocation.Name.Equals(table.CurrentLocation))
            {
                Vector2 offset = new Vector2(0,
                    (float)Math.Round(4f * Math.Sin(Game1.currentGameTime.TotalGameTime.TotalMilliseconds / 250.0)));

                foreach (Seat seat in table.Seats)
                {
                    if (seat.ReservingCustomer is { ItemToOrder: not null } customer)
                    {
                        Vector2 localPosition = customer.getLocalPosition(Game1.viewport);
                        localPosition.Y -= 32 + customer.Sprite.SpriteHeight * 3;

                        e.SpriteBatch.Draw(
                            Game1.emoteSpriteSheet,
                            localPosition + offset,
                            new Rectangle(32, 0, 16, 16),
                            Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 1f);

                        customer.ItemToOrder.drawInMenu(e.SpriteBatch, localPosition + offset, 0.35f, 1f, 1f);
                    }
                }

                e.SpriteBatch.Draw(
                    Game1.mouseCursors,
                    Game1.GlobalToLocal(table.Center + new Vector2(-8, -64)) + offset,
                    new Rectangle(402, 495, 7, 16),
                    Color.Crimson,
                    0f,
                    new Vector2(1f, 4f),
                    4f,
                    SpriteEffects.None,
                    1f);
            }
        }
    }

    internal Table GetTableOfSeat(Seat seat)
    {
        return Tables.FirstOrDefault(t => t.Seats.Contains(seat));
    }

    internal void OnFurnitureListChanged(object sender, FurnitureListChangedEventArgs e)
    {
        if (e.Location.Equals(Indoor) || e.Location.Equals(Outdoor))
        {
            foreach (var f in e.Removed)
            {
                if (Utility.IsChair(f))
                {
                    FurnitureSeat trackedChair = Tables
                        .OfType<FurnitureTable>()
                        .SelectMany(t => t.Seats)
                        .OfType<FurnitureSeat>()
                        .FirstOrDefault(seat => seat.ActualChair.Value.Equals(f));

                    if (trackedChair?.Table is FurnitureTable table)
                    {
                        if (table.IsReserved.Value)
                            Log.Warn("Removed a chair but the table was reserved");

                        table.RemoveChair(f);
                    }
                }
                else if (Utility.IsTable(f))
                {
                    if (Utility.IsTableTracked(f, e.Location, out FurnitureTable trackedTable))
                    {
                        RemoveTable(trackedTable);
                    }
                }
            }
            foreach (var f in e.Added)
            {
                if (Utility.IsChair(f))
                {
                    // Get position of table in front of the chair
                    Vector2 tablePos = f.TileLocation + Utility.DirectionIntToDirectionVector(f.currentRotation.Value) * new Vector2(1, -1);

                    // Get table Furniture object
                    Furniture facingFurniture = e.Location.GetFurnitureAt(tablePos);

                    if (facingFurniture == null ||
                        !Utility.IsTable(facingFurniture) ||
                        facingFurniture
                            .GetBoundingBox()
                            .Intersects(f.boundingBox.Value)) // if chair was placed on top of the table
                    {
                        continue;
                    }

                    FurnitureTable table;

                    if (Utility.IsTableTracked(facingFurniture, e.Location, out FurnitureTable existing))
                        table = existing;
                    else
                        table = new FurnitureTable(facingFurniture, e.Location.Name);

                    if (TryAddTable(table))
                        table.AddChair(f);
                }
                else if (Utility.IsTable(f))
                {
                    if (!Utility.IsTableTracked(f, e.Location, out _))
                    {
                        FurnitureTable table = new FurnitureTable(f, e.Location.Name);
                        TryAddTable(table);
                    }
                    
                }
            }
        }
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
                        Enabled = true;
                        return false;
                    }
                }

                return true;
            });
        }

        return foundSignboard || foundIndoor;
    }

    internal void OnTimeChanged(object sender, TimeChangedEventArgs e)
    {
        int minutesTillCloses = SUtility.CalculateMinutesBetweenTimes(Game1.timeOfDay, ClosingTime.Value);
        int minutesTillOpens = SUtility.CalculateMinutesBetweenTimes(Game1.timeOfDay, OpeningTime.Value);
        int minutesSinceLastVisitors = SUtility.CalculateMinutesBetweenTimes(LastTimeCustomersArrived, Game1.timeOfDay);
        float percentageOfTablesFree = (float)Mod.Cafe.Tables.Count(t => !t.IsReserved.Value) / Mod.Cafe.Tables.Count();

        if (minutesTillCloses <= 20)
            return;

        float prob = 0f;

        // more chance if it's been a while since last Visitors
        prob += minutesSinceLastVisitors switch
        {
            <= 20 => 0f,
            <= 30 => Game1.random.Next(5) == 0 ? 0.05f : -0.1f,
            <= 60 => Game1.random.Next(2) == 0 ? 0.1f : 0f,
            _ => 0.25f
        };

        // more chance if a higher percent of tables are free
        prob += percentageOfTablesFree switch
        {
            <= 0.2f => 0.0f,
            <= 0.5f => 0.1f,
            <= 0.8f => 0.15f,
            _ => 0.2f
        };

        // slight chance to spawn if last hour of open time
        if (minutesTillCloses <= 60)
            prob += Game1.random.Next(20 + Math.Max(0, minutesTillCloses / 3)) >= 28 ? 0.2f : -0.5f;
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
                    route = new List<string>() { "BusStop" };
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
