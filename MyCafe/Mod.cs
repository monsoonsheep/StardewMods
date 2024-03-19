using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Xml.Serialization;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using MonsoonSheep.Stardew.Common;
using MonsoonSheep.Stardew.Common.Patching;
using MyCafe.Characters;
using MyCafe.Characters.Spawning;
using MyCafe.Data;
using MyCafe.Data.Customers;
using MyCafe.Data.Models;
using MyCafe.Data.Models.Appearances;
using MyCafe.Enums;
using MyCafe.Interfaces;
using MyCafe.Inventories;
using MyCafe.Locations.Objects;
using MyCafe.Patching;
using MyCafe.UI;
using Netcode;
using StardewModdingAPI;
using StardewModdingAPI.Enums;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.GameData.Buildings;
using StardewValley.Inventories;
using StardewValley.Locations;
using StardewValley.Objects;
using xTile;
using SUtility = StardewValley.Utility;

namespace MyCafe;

public class Mod : StardewModdingAPI.Mod
{
    internal static Mod Instance = null!;

    private Texture2D _sprites = null!;
    private ConfigModel _loadedConfig = null!;

    private AssetManager _assetManager = null!;
    private CharacterFactory _characterFactory = null!;

    internal NetRef<Cafe> CafeField = [];

    internal static bool IsPlacingSignBoard;

    internal static HashSet<string> NpcCustomers = new();

    internal static Texture2D Sprites
        => Instance._sprites;

    internal static string UniqueId
        => Instance.ModManifest.UniqueID;

    internal static Cafe Cafe
        => Instance.CafeField.Value;

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
        CafeState.Register(spaceCore);

        this._assetManager.LoadContent(this.Helper.ContentPacks.CreateTemporary(
            Path.Combine(this.Helper.DirectoryPath, "assets", "DefaultContent"),
            $"{this.ModManifest.Author}.DefaultContent",
            "MyCafe Fake Content Pack",
            "Default content for MyCafe",
            this.ModManifest.Author,
            this.ModManifest.Version)
        );

        this._characterFactory.Initialize();
    }

    private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
    {
        if (!Context.IsMainPlayer)
            return;

        this.LoadCafeData();
        Cafe.InitializeForHost(this.Helper);
        Pathfinding.AddRoutesToFarm();
    }

    internal void OnDayStarted(object? sender, DayStartedEventArgs e)
    {
        if (!Context.IsMainPlayer)
            return;

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
        SUtility.ForEachLocation(delegate (GameLocation loc)
        {
            for (int i = loc.characters.Count - 1; i >= 0; i--)
                if (loc.characters[i].Name.StartsWith(ModKeys.CUSTOMER_NPC_NAME_PREFIX))
                    loc.characters.RemoveAt(i); // TODO if it's a villager customer, convert them (is it needed or will the game warp them anyway?)
            return true;
        });
        Cafe.RemoveAllCustomers();
    }

    internal void OnTimeChanged(object? sender, TimeChangedEventArgs e)
    {
        if (!Context.IsMainPlayer) return;
        Cafe.TenMinuteUpdate();
    }

    [SuppressMessage("ReSharper", "PossibleLossOfFraction", Justification = "Deliberate in order to get the tile")]
    internal void OnRenderedWorld(object? sender, RenderedWorldEventArgs e)
    {
        // Get list of reserved tables with center coords
        foreach (var table in Cafe.Tables)
        {
            if (Game1.currentLocation.Name.Equals(table.CurrentLocation))
            {
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
                }
            }
        }

        if (IsPlacingSignBoard)
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
        if (!Context.IsMainPlayer)
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

    internal void LoadCafeData()
    {
        // Load cafe data
        if (!Game1.IsMasterGame || string.IsNullOrEmpty(Constants.CurrentSavePath) || !File.Exists(Path.Combine(Constants.CurrentSavePath, "MyCafe", "cafedata")))
            return;

        string cafeDataPath = Path.Combine(Constants.CurrentSavePath, "MyCafe", "cafedata");
        CafeArchiveData cafeData;
        XmlSerializer serializer = new XmlSerializer(typeof(CafeArchiveData));

        try
        {
            using StreamReader reader = new StreamReader(cafeDataPath);
            cafeData = (CafeArchiveData) serializer.Deserialize(reader)!;
        }
        catch (InvalidOperationException e)
        {
            Log.Error("Couldn't read Cafe Data from external save folder");
            Log.Error($"{e.Message}\n{e.StackTrace}");
            return;
        }

        Cafe.OpeningTime.Set(cafeData.OpeningTime);
        Cafe.ClosingTime.Set(cafeData.ClosingTime);
        Cafe.Menu.Menu.Set(cafeData.MenuItemLists);
        foreach (VillagerCustomerData data in cafeData.VillagerCustomersData)
        {
            if (Assets.VillagerVisitors.TryGetValue(data.NpcName, out VillagerCustomerModel? model))
            {
                data.Model = model;
                Cafe.VillagerCustomers.VillagerData.Add(data.NpcName, data);
            }
        }
    }

    internal void SaveCafeData()
    {
        if (string.IsNullOrEmpty(Constants.CurrentSavePath))
            return;
        
        string externalSaveFolderPath = Path.Combine(Constants.CurrentSavePath, "MyCafe");
        string cafeDataPath = Path.Combine(Constants.CurrentSavePath, "MyCafe", "cafedata");

        CafeArchiveData cafeData = new()
        {
            OpeningTime = Cafe.OpeningTime.Value,
            ClosingTime = Cafe.ClosingTime.Value,
            MenuItemLists = new SerializableDictionary<MenuCategory, Inventory>(Cafe.Menu.ItemDictionary),
            VillagerCustomersData = Cafe.VillagerCustomers?.VillagerData.Values.ToList() ?? []
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

    internal void InitializeGmcm(IModHelper helper, IManifest manifest)
    {
        // get Generic Mod Config Menu's API (if it's installed)
        IGenericModConfigMenuApi? configMenu = helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");

        // register mod
        configMenu?.Register(
            mod: manifest,
            reset: () => this._loadedConfig = new ConfigModel(),
            save: () => helper.WriteConfig(this._loadedConfig)
        );
    }

    internal static IBusSchedulesApi? GetBusSchedulesApi()
    {
        return Instance.Helper.ModRegistry.GetApi<IBusSchedulesApi>("MonsoonSheep.BusSchedules");
    }

#if YOUTUBE || TWITCH

    internal static LiveChatManager ChatManager = new();

    internal void InitializeLiveChat()
    {
        Cafe.ChatCustomers.Initialize(this.Helper);
    }
#endif
}
