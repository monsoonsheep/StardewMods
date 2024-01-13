using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MyCafe.ChairsAndTables;
using MyCafe.Interfaces;
using MyCafe.Patching;
using Netcode;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Objects;
using System;
using System.Collections.Generic;
using System.Linq;
using SUtility = StardewValley.Utility;

namespace MyCafe;

public class Mod : StardewModdingAPI.Mod
{
    internal new static IMonitor Monitor;
    internal static IModHelper ModHelper;
    internal new static IManifest ModManifest;

    internal static NetRef<Cafe> NetCafe = new NetRef<Cafe>(new Cafe());
    internal static Cafe Cafe
        => NetCafe.Value;

    internal static Texture2D Sprites;

    /// <inheritdoc/>
    public override void Entry(IModHelper helper)
    {
        Monitor = base.Monitor;
        ModHelper = helper;
        ModManifest = base.ModManifest;
        Log.Monitor = Monitor;
        I18n.Init(helper.Translation);
        ModConfig.LoadedConfig = helper.ReadConfig<ConfigModel>();

        // Harmony patches
        try
        {
            var harmony = new Harmony(ModManifest.UniqueID);
            new List<PatchCollection>
            {
                new CharacterPatches(), new GameLocationPatches()
            }.ForEach(l => l.ApplyAll(harmony));
        }
        catch (Exception e)
        {
            Log.Debug($"Couldn't patch methods - {e}", LogLevel.Error);
            return;
        }
        helper.Events.GameLoop.GameLaunched += OnGameLaunched;
        helper.Events.Content.AssetRequested += AssetManager.OnAssetRequested;
        helper.Events.Content.AssetReady += AssetManager.OnAssetReady;

        helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
        helper.Events.GameLoop.DayStarted += DayUpdate;
        helper.Events.GameLoop.TimeChanged += OnTimeChanged;
        helper.Events.Display.RenderedWorld += OnRenderedWorld;
        helper.Events.World.FurnitureListChanged += OnFurnitureListChanged;
        helper.Events.Multiplayer.PeerConnected += Sync.OnPeerConnected;
        helper.Events.Multiplayer.ModMessageReceived += Sync.OnModMessageReceived;
        helper.Events.Input.ButtonPressed += Debug.ButtonPress;

        Sprites = helper.ModContent.Load<Texture2D>("assets/sprites.png");
    }


    private void OnGameLaunched(object sender, GameLaunchedEventArgs e)
    {
        ISpaceCoreApi spacecore = ModHelper.ModRegistry.GetApi<ISpaceCoreApi>("spacechase0.SpaceCore");
        if (spacecore == null)
        {
            Log.Error("SpaceCore not found.");
            return;
        }

        spacecore.RegisterSerializerType(typeof(Cafe));
        spacecore.RegisterCustomProperty(typeof(Farm), "Cafe", typeof(NetRef<Cafe>),
            AccessTools.Method(typeof(CafeSyncExtensions), nameof(CafeSyncExtensions.get_Cafe)),
            AccessTools.Method(typeof(CafeSyncExtensions), nameof(CafeSyncExtensions.set_Cafe)));

        ModConfig.InitializeGmcm();

        GameLocation.RegisterTileAction(ModKeys.SIGNBOARD_BUILDING_CLICK_EVENT_KEY, delegate
        {
            // TODO do the thing
            return true;
        });
    }

    private void OnSaveLoaded(object sender, SaveLoadedEventArgs e)
    {
        Cafe.Initialize(ModHelper);
    }


    internal void DayUpdate(object sender, DayStartedEventArgs e)
    {
        if (Context.IsMainPlayer)
        {
            if (Cafe.UpdateCafeLocations() is true)
            {
                Cafe.Enabled = true;
                Cafe.PopulateTables();
                Cafe.PopulateRoutesToCafe();
            }
            else if (Cafe.Enabled)
                Cafe.Enabled = false;
        }
        if (Cafe.Enabled)
            Cafe.DayUpdate();
    }

    internal void OnTimeChanged(object sender, TimeChangedEventArgs e)
    {
        int minutesTillCloses = SUtility.CalculateMinutesBetweenTimes(Game1.timeOfDay, Cafe.ClosingTime.Value);
        int minutesTillOpens = SUtility.CalculateMinutesBetweenTimes(Game1.timeOfDay, Cafe.OpeningTime.Value);
        int minutesSinceLastVisitors = SUtility.CalculateMinutesBetweenTimes(Cafe.LastTimeCustomersArrived, Game1.timeOfDay);
        float percentageOfTablesFree = (float)Mod.Cafe.Tables.Count(t => !t.IsReserved) / Cafe.Tables.Count();

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


    internal void OnRenderedWorld(object sender, RenderedWorldEventArgs e)
    {
        // get list of reserved tables with center coords
        foreach (var table in Cafe.Tables)
        {
            if (Game1.currentLocation.Name.Equals(table.CurrentLocation))
            {
                Vector2 offset = new Vector2(0,
                    (float)Math.Round(4f * Math.Sin(Game1.currentGameTime.TotalGameTime.TotalMilliseconds / 250.0)));

                if (table.State.Value == TableState.CustomersDecidedOnOrder)
                {
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
                else if (table.State.Value == TableState.CustomersWaitingForFood)
                {
                    foreach (Seat seat in table.Seats)
                    {
                        if (seat.ReservingCustomer is { ItemToOrder.Value: not null } customer)
                        {
                            Vector2 pos = customer.getLocalPosition(Game1.viewport);
                            pos.Y -= 32 + customer.Sprite.SpriteHeight * 3;

                            e.SpriteBatch.Draw(
                                Game1.emoteSpriteSheet,
                                pos + offset,
                                new Rectangle(32, 0, 16, 16),
                                Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 1f);

                            customer.ItemToOrder.Value.drawInMenu(e.SpriteBatch, pos + offset, 0.35f, 1f, 1f);
                        }
                    }
                }
            }

        }
    }

    internal void OnFurnitureListChanged(object sender, FurnitureListChangedEventArgs e)
    {
        if (e.Location.Equals(Cafe.Indoor) || e.Location.Equals(Cafe.Outdoor))
        {
            foreach (var f in e.Removed)
            {
                if (Utility.IsChair(f))
                {
                    FurnitureSeat trackedChair = Cafe.Tables
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
                    if (Utility.IsTableTracked(f, e.Location, out FurnitureTable trackedTable))
                    {
                        Cafe.RemoveTable(trackedTable);
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

                    if (Cafe.TryAddTable(table))
                        table.AddChair(f);
                }
                else if (Utility.IsTable(f))
                {
                    if (!Utility.IsTableTracked(f, e.Location, out _))
                    {
                        FurnitureTable table = new FurnitureTable(f, e.Location.Name);
                        Cafe.TryAddTable(table);
                    }

                }
            }
        }
    }

}
