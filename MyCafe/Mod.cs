using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Serialization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonsoonSheep.Stardew.Common;
using MonsoonSheep.Stardew.Common.Patching;
using MyCafe.Characters;
using MyCafe.Data;
using MyCafe.Data.Customers;
using MyCafe.Data.Models;
using MyCafe.Enums;
using MyCafe.Interfaces;
using MyCafe.Inventories;
using MyCafe.Locations.Objects;
using MyCafe.Netcode;
using MyCafe.Patching;
using MyCafe.UI;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.Inventories;
using StardewValley.Locations;

namespace MyCafe;

public class Mod : StardewModdingAPI.Mod
{
    internal static Mod Instance = null!;

    private Cafe _cafe = null!;

    private Texture2D _sprites = null!;
    private ConfigModel _loadedConfig = null!;

    private AssetManager _assetManager = null!;
    private CharacterFactory _characterFactory = null!;

    internal bool IsPlacingSignBoard;

    internal static Texture2D Sprites
        => Instance._sprites;

    internal static string UniqueId
        => Instance.ModManifest.UniqueID;

    internal static Cafe Cafe
        => Instance._cafe;

    internal static ConfigModel Config
        => Instance._loadedConfig;

    internal static CharacterFactory CharacterFactory
        => Instance._characterFactory;

    internal static AssetManager Assets
        => Instance._assetManager;

    public Mod()
    {
        Instance = this;
    }

    /// <inheritdoc/>
    public override void Entry(IModHelper helper)
    {
        Log.Monitor = this.Monitor;
        I18n.Init(helper.Translation);
        this._loadedConfig = helper.ReadConfig<ConfigModel>();
        this._assetManager = new AssetManager(helper);
        this._cafe = new Cafe();
        this._characterFactory = new CharacterFactory(helper);
        this._sprites = helper.ModContent.Load<Texture2D>(Path.Combine("assets", "sprites.png"));

        // Harmony patches
        if (HarmonyPatcher.TryApply(this,
                new ActionPatcher(),
                new LocationPatcher(),
                new NetFieldPatcher(),
                new CharacterPatcher(),
                new FurniturePatcher(),
                new SignboardPatcher()
            ) is false)
            return;

        IModEvents events = helper.Events;

        events.GameLoop.GameLaunched += this.OnGameLaunched;
        events.GameLoop.SaveLoaded += this.OnSaveLoaded;
        events.GameLoop.DayStarted += this.OnDayStarted;
        events.GameLoop.TimeChanged += this.OnTimeChanged;
        events.GameLoop.DayEnding += this.OnDayEnding;
        events.GameLoop.Saving += this.OnSaving;

        events.Content.AssetRequested += this._assetManager.OnAssetRequested;
        events.Content.AssetReady += this._assetManager.OnAssetReady;
        events.Multiplayer.ModMessageReceived += this.OnModMessageReceived;
        events.Display.RenderedWorld += this.OnRenderedWorld;
        events.World.FurnitureListChanged += this.OnFurnitureListChanged;
        events.Input.ButtonPressed += Debug.ButtonPress;
    }

    private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
    {
        this.InitializeGmcm(this.Helper, this.ModManifest);

        GameLocation.RegisterTileAction(ModKeys.SIGNBOARD_BUILDING_CLICK_EVENT_KEY, CafeMenu.Action_OpenCafeMenu);

        ISpaceCoreApi spaceCore = this.Helper.ModRegistry.GetApi<ISpaceCoreApi>("spacechase0.SpaceCore")!;
        spaceCore.RegisterSerializerType(typeof(CafeLocation));

        // Remove this? It might not be need. Test multiplayer to confirm.
        FarmerTeamVirtualProperties.Register(spaceCore);

        this._assetManager.LoadContent(this.Helper.ContentPacks.CreateTemporary(
            Path.Combine(this.Helper.DirectoryPath, "assets", "DefaultContent"),
            $"{this.ModManifest.Author}.DefaultContent",
            "MyCafe Fake Content Pack",
            "Default content for MyCafe",
            this.ModManifest.Author,
            this.ModManifest.Version)
        );
    }

    private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
    {
        if (!Context.IsMainPlayer)
            return;

        Cafe.InitializeForHost(this.Helper);
        this.LoadCafeData();
        Pathfinding.AddRoutesToFarm();
        this.CleanUpCustomers();

    }

    internal void OnDayStarted(object? sender, DayStartedEventArgs e)
    {
        if (!Context.IsMainPlayer)
            return;

        // When the signboard is built, check if player has flag, add if not and inject the event with AssetRequested)
        if (Game1.IsBuildingConstructed(ModKeys.CAFE_SIGNBOARD_BUILDING_ID) &&
            Game1.MasterPlayer.mailReceived.Add(ModKeys.MAILFLAG_HAS_BUILT_SIGNBOARD) == true)
        {
            GameLocation eventLocation = Game1.locations.First(l => l.isBuildingConstructed(ModKeys.CAFE_SIGNBOARD_BUILDING_ID));
            this.Helper.GameContent.InvalidateCache($"Data/Events/{eventLocation.Name}");
        }

        Cafe.DayUpdate();
    }

    internal void OnSaving(object? sender, SavingEventArgs e)
    {
        if (!Context.IsMainPlayer)
            return;

        this.SaveCafeData();
    }

    internal void OnDayEnding(object? sender, DayEndingEventArgs e)
    {
        if (!Context.IsMainPlayer)
            return;

        // Delete customers
        Cafe.RemoveAllCustomers();
        this.CleanUpCustomers();
    }

    internal void OnTimeChanged(object? sender, TimeChangedEventArgs e)
    {
        if (!Context.IsMainPlayer)
            return;
        Cafe.TenMinuteUpdate();
    }

    [SuppressMessage("ReSharper", "PossibleLossOfFraction", Justification = "Deliberate in order to get the tile")]
    internal void OnRenderedWorld(object? sender, RenderedWorldEventArgs e)
    {
        if (Cafe.Enabled)
        {
            // Get list of reserved tables with center coords
            foreach (Table table in Cafe.Tables)
            {
                if (Game1.currentLocation.Name.Equals(table.CurrentLocation))
                {
                    // Table status
                    Vector2 offset = new Vector2(0, (float) Math.Round(4f * Math.Sin(Game1.currentGameTime.TotalGameTime.TotalMilliseconds / 250.0)));

                    switch (table.State.Value)
                    {
                        case TableState.CustomersDecidedOnOrder:
                            // Exclamation mark
                            e.SpriteBatch.Draw(
                                Game1.mouseCursors,
                                Game1.GlobalToLocal(table.Center + new Vector2(-8, -64)) + offset,
                                new Rectangle(402, 495, 7, 16),
                                Color.White,
                                0f,
                                new Vector2(1f, 4f),
                                4f,
                                SpriteEffects.None,
                                1f);
                            break;
                        case TableState.Free:
                            if (true || Game1.timeOfDay < Cafe.OpeningTime)
                            {
                                e.SpriteBatch.DrawString(Game1.tinyFont, table.Seats.Count.ToString(), Game1.GlobalToLocal(table.Center + new Vector2(-12, -112)) + offset, Color.LightBlue, 0f, Vector2.Zero, 5f, SpriteEffects.None, 0.99f);
                                e.SpriteBatch.DrawString(Game1.tinyFont, table.Seats.Count.ToString(), Game1.GlobalToLocal(table.Center + new Vector2(-10, -96)) + offset, Color.Black, 0f, Vector2.Zero, 4f, SpriteEffects.None, 1f);
                            }

                            break;

                    }
                }
            }
        }

        if (this.IsPlacingSignBoard)
        {
            foreach (Vector2 tile in TileHelper.GetCircularTileGrid(new Vector2((Game1.viewport.X + Game1.getOldMouseX(false)) / 64, (Game1.viewport.Y + Game1.getOldMouseY(false)) / 64), this._loadedConfig.DistanceForSignboardToRegisterTables))
            {
                // get tile area in screen pixels
                Rectangle area = new((int)(tile.X * Game1.tileSize - Game1.viewport.X), (int)(tile.Y * Game1.tileSize - Game1.viewport.Y), Game1.tileSize, Game1.tileSize);

                // choose tile color
                Color color = Color.LightCyan;

                // draw background
                e.SpriteBatch.DrawLine(area.X, area.Y, new Vector2(area.Width, area.Height), color * 0.2f);

                // draw border
                Color borderColor = color * 0.5f;
                e.SpriteBatch.DrawLine(area.X, area.Y, new Vector2(area.Width, 1), borderColor); // top
                e.SpriteBatch.DrawLine(area.X, area.Y, new Vector2(1, area.Height), borderColor); // left
                e.SpriteBatch.DrawLine(area.X + area.Width, area.Y, new Vector2(1, area.Height), borderColor); // right
                e.SpriteBatch.DrawLine(area.X, area.Y + area.Height, new Vector2(area.Width, 1), borderColor); // bottom
            }
        }

    }

    internal void OnFurnitureListChanged(object? sender, FurnitureListChangedEventArgs e)
    {
        if (!Context.IsMainPlayer || Cafe.Enabled == false)
            return;

        if (e.Location.Equals(Cafe.Indoor) || e.Location.Equals(Cafe.Outdoor))
        {
            foreach (var f in e.Removed)
                Cafe.OnFurnitureRemoved(f, e.Location);
            
            foreach (var f in e.Added)
                Cafe.OnFurniturePlaced(f, e.Location);
        }
    }

    internal void OnModMessageReceived(object? sender, ModMessageReceivedEventArgs e)
    {
        if (e.FromModID != this.ModManifest.UniqueID)
            return;

        if (e.Type == "CustomerDoEmote" && !Context.IsMainPlayer)
        {
            try
            {
                (string key, int emote) = e.ReadAs<(string, int)>();
                Game1.getCharacterFromName(key)?.doEmote(emote);
            }
            catch (InvalidOperationException ex)
            {
                Log.Debug($"Invalid message from host\n{ex}", LogLevel.Error);
            }
        }
    }

    private void CleanUpCustomers()
    {
        StardewValley.Utility.ForEachLocation((loc) =>
        {
            for (int i = loc.characters.Count - 1; i >= 0; i--)
            {
                NPC npc = loc.characters[i];
                if (npc.Name.StartsWith(ModKeys.CUSTOMER_NPC_NAME_PREFIX))
                {
                    loc.characters.RemoveAt(i);
                }
            }

            return true;
        });
    }

    internal void LoadCafeData()
    {
        string cafeDataFile = Path.Combine(Constants.CurrentSavePath ?? "", "MyCafe", "cafedata");

        // Load cafe data
        if (!Game1.IsMasterGame || string.IsNullOrEmpty(Constants.CurrentSavePath) || !File.Exists(cafeDataFile))
            return;

        CafeArchiveData cafeData;
        XmlSerializer serializer = new XmlSerializer(typeof(CafeArchiveData));

        try
        {
            using StreamReader reader = new StreamReader(cafeDataFile);
            cafeData = (CafeArchiveData) serializer.Deserialize(reader)!;
        }
        catch (InvalidOperationException e)
        {
            Log.Error("Couldn't read Cafe Data from external save folder");
            Log.Error($"{e.Message}\n{e.StackTrace}");
            return;
        }

        Cafe.OpeningTime = cafeData.OpeningTime;
        Cafe.ClosingTime = cafeData.ClosingTime;
        Cafe.Menu.MenuObject.Set(cafeData.MenuItemLists);
        foreach (VillagerCustomerData data in cafeData.VillagerCustomersData)
        {
            if (!Assets.VillagerCustomerModels.TryGetValue(data.NpcName, out VillagerCustomerModel? model))
            {
                Log.Debug("Loading NPC customer data but not model found. Skipping...");
                continue;
            }

            Log.Trace($"Loading customer data from save file {model.NpcName}");
            data.NpcName = model.NpcName;
            Cafe.VillagerData[data.NpcName] = data;
        }
    }

    internal void SaveCafeData()
    {
        if (string.IsNullOrEmpty(Constants.CurrentSavePath))
            return;
        
        string externalSaveFolderPath = Path.Combine(Constants.CurrentSavePath, "MyCafe");
        string cafeDataPath = Path.Combine(externalSaveFolderPath, "cafedata");

        CafeArchiveData cafeData = new()
        {
            OpeningTime = Cafe.OpeningTime,
            ClosingTime = Cafe.ClosingTime,
            MenuItemLists = new SerializableDictionary<FoodCategory, Inventory>(Cafe.Menu.ItemDictionary),
            VillagerCustomersData = Cafe.VillagerData.Values.ToList() ?? []
        };

        XmlSerializer serializer = new(typeof(CafeArchiveData));
        try
        {
            // Create the MyCafe folder near the save file, if one doesn't exist
            if (!Directory.Exists(externalSaveFolderPath))
                Directory.CreateDirectory(externalSaveFolderPath);

            using StreamWriter reader = new StreamWriter(cafeDataPath);
            serializer.Serialize(reader, cafeData);
        }
        catch
        {
            Log.Error("Couldn't read Cafe Data from external save folder");
        }
    }

    private void TryLoadCategoriesConfig()
    {

    }

    internal void InitializeGmcm(IModHelper helper, IManifest manifest)
    {
        // get Generic Mod Config Menu's API (if it's installed)
        IGenericModConfigMenuApi? configMenu = helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
        if (configMenu == null)
            return;

        // register mod
        configMenu.Register(
            mod: manifest,
            reset: () => this._loadedConfig = new ConfigModel(),
            save: () => helper.WriteConfig(this._loadedConfig)
            );

        configMenu.AddBoolOption(
            mod: manifest,
            getValue: () => this._loadedConfig.ShowPricesInFoodMenu,
            setValue: (value) => this._loadedConfig.ShowPricesInFoodMenu = value,
            name: () => "Show Prices in Food Menu");

        configMenu.AddSectionTitle(
            mod: manifest,
            text: () => "Customer Visits"
            );

        configMenu.AddNumberOption(
            mod: manifest,
            getValue: () => this._loadedConfig.EnableNpcCustomers,
            setValue: (value) => this._loadedConfig.EnableNpcCustomers = value,
            name: () => "NPC Customers",
            tooltip: () => "How often villager/townspeople customers will visit",
            min: 0,
            max: 5,
            interval: 1,
            formatValue: (val) => val == 0 ? "Disabled" : val.ToString()
        );

        configMenu.AddNumberOption(
            mod: manifest,
            getValue: () => this._loadedConfig.EnableCustomCustomers,
            setValue: (value) => this._loadedConfig.EnableCustomCustomers = value,
            name: () => "Custom Customers",
            tooltip: () => "How often custom-made customers will visit",
            min: 0,
            max: 5,
            interval: 1,
            formatValue: (val) => val == 0 ? "Disabled" : val.ToString()
            );

        configMenu.AddNumberOption(
            mod: manifest,
            getValue: () => this._loadedConfig.EnableRandomlyGeneratedCustomers,
            setValue: (value) => this._loadedConfig.EnableRandomlyGeneratedCustomers = value,
            name: () => "Randomly Generated Customers",
            tooltip: () => "How often randomly generated customers will visit",
            min: 0,
            max: 5,
            interval: 1,
            formatValue: (val) => val == 0 ? "Disabled" : val.ToString()
            );

        configMenu.AddSectionTitle(
            mod: manifest,
            text: () => "Tables"
            );

        configMenu.AddNumberOption(
            mod: manifest,
            getValue: () => this._loadedConfig.DistanceForSignboardToRegisterTables,
            setValue: (value) => this._loadedConfig.DistanceForSignboardToRegisterTables = value,
            name: () => "Distance to register tables",
            tooltip: () => "Radius from the signboard to detect tables",
            min: 3,
            max: 25,
            interval: 1,
            formatValue: (val) => val + " tiles"
            );
    }

    internal static IBusSchedulesApi? GetBusSchedulesApi()
    {
        return Instance.Helper.ModRegistry.GetApi<IBusSchedulesApi>("MonsoonSheep.BusSchedules");
    }

    internal static string GetCafeIntroductionEvent()
    {
        GameLocation eventLocation = Game1.locations.First(l => l.isBuildingConstructed(ModKeys.CAFE_SIGNBOARD_BUILDING_ID));
        Building signboard = eventLocation.getBuildingByType(ModKeys.CAFE_SIGNBOARD_BUILDING_ID);
        Point signboardTile = new Point(signboard.tileX.Value, signboard.tileY.Value + signboard.tilesHigh.Value);

        string eventString = Game1.content.Load<Dictionary<string, string>>($"Data/{ModKeys.MODASSET_EVENTS}")[ModKeys.EVENT_CAFEINTRODUCTION];

        // Replace the encoded coordinates with the position of the signboard building
        string substituted = Regex.Replace(
            eventString,
            @"(6\d{2})\s(6\d{2})",
            (m) => $"{(int.Parse(m.Groups[1].Value) - 650 + signboardTile.X)} {(int.Parse(m.Groups[2].Value) - 650 + signboardTile.Y)}");

        return substituted;
    }

#if YOUTUBE || TWITCH

    internal static LiveChatManager ChatManager = new();

    internal void InitializeLiveChat()
    {
        Cafe.ChatCustomers = new RandomCustomerSpawner(getModelFunc: CharacterFactory.CreateRandomCustomer);
        Cafe.ChatCustomers.Initialize(this.Helper);
    }
#endif
}
