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
using Microsoft.Xna.Framework.Graphics;
using MonsoonSheep.Stardew.Common;
using MonsoonSheep.Stardew.Common.Patching;
using MyCafe.Characters;
using MyCafe.Characters.CharacterGeneration;
using MyCafe.Data;
using MyCafe.Data.Customers;
using MyCafe.Data.Models;
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
    internal AssetManager AssetManager = null!;
    internal static string UniqueId = null!;

    private ConfigModel LoadedConfig = null!;
    internal static Texture2D Sprites = null!;

    internal NetRef<Cafe> CafeField = [];

    internal Dictionary<string, BusCustomerData> CustomersData = [];

    internal static bool IsPlacingSignBoard;

    internal static Cafe Cafe
        => Instance.CafeField.Value;

    internal static ConfigModel Config
        => Instance.LoadedConfig;

    public Mod()
    {
        Instance = this;
    }

    /// <inheritdoc/>
    public override void Entry(IModHelper helper)
    {
        Log.Monitor = this.Monitor;
        I18n.Init(helper.Translation);
        this.LoadedConfig = helper.ReadConfig<ConfigModel>();
        UniqueId = this.ModManifest.UniqueID;
        this.AssetManager = new AssetManager(this.Helper);

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

        events.Content.AssetRequested += this.AssetManager.OnAssetRequested;
        events.Content.AssetReady += this.AssetManager.OnAssetReady;
        events.Multiplayer.ModMessageReceived += this.OnModMessageReceived;
        events.Display.RenderedWorld += this.OnRenderedWorld;
        events.World.FurnitureListChanged += this.OnFurnitureListChanged;
        events.Input.ButtonPressed += Debug.ButtonPress;

        Sprites = helper.ModContent.Load<Texture2D>(Path.Combine("assets", "sprites.png"));
    }

    private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
    {
        this.InitializeGmcm(this.Helper, this.ModManifest);

        GameLocation.RegisterTileAction(ModKeys.SIGNBOARD_BUILDING_CLICK_EVENT_KEY, CafeMenu.Action_OpenCafeMenu);

        ISpaceCoreApi spaceCore = this.Helper.ModRegistry.GetApi<ISpaceCoreApi>("spacechase0.SpaceCore")!;
        spaceCore.RegisterSerializerType(typeof(CafeLocation));
        CafeState.Register(spaceCore);

        // Load content packs
        foreach (IContentPack contentPack in this.Helper.ContentPacks.GetOwned())
            this.AssetManager.LoadContentPack(contentPack);

        // Load default content pack included in assets folder
        this.AssetManager.LoadContentPack(
            this.Helper.ContentPacks.CreateTemporary(
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

        this.LoadCafeData();
        Cafe.Initialize(this.Helper, this.CustomersData);
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

        SUtility.ForEachLocation(delegate (GameLocation loc)
        {
            for (int i = loc.characters.Count - 1; i >= 0; i--)
                if (loc.characters[i] is Customer)
                    loc.characters.RemoveAt(i);
            return true;
        });

        Cafe.Customers.RemoveAllCustomers();
    }

    internal void OnTimeChanged(object? sender, TimeChangedEventArgs e)
    {
        if (!Context.IsMainPlayer) return;

        int minutesTillCloses = SUtility.CalculateMinutesBetweenTimes(Game1.timeOfDay, Cafe.ClosingTime.Value);
        int minutesTillOpens = SUtility.CalculateMinutesBetweenTimes(Game1.timeOfDay, Cafe.OpeningTime.Value);
        int minutesSinceLastVisitors = SUtility.CalculateMinutesBetweenTimes(Cafe.LastTimeCustomersArrived, Game1.timeOfDay);
        float percentageOfTablesFree = (float)Cafe.Tables.Count(t => !t.IsReserved) / Cafe.Tables.Count;

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

        Log.Info(prob.ToString(CultureInfo.CurrentCulture));
    }

    [SuppressMessage("ReSharper", "PossibleLossOfFraction", Justification = "Deliberate in order to get the tile")]
    internal void OnRenderedWorld(object? sender, RenderedWorldEventArgs e)
    {
        // get list of reserved tables with center coords
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
            foreach (Vector2 tile in TileHelper.GetCircularTileGrid(new Vector2((Game1.viewport.X + Game1.getOldMouseX(false)) / 64, (Game1.viewport.Y + Game1.getOldMouseY(false)) / 64), this.LoadedConfig.DistanceForSignboardToRegisterTables))
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
        if (!Context.IsMainPlayer) return;

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

        if (e.Type == "VisitorDoEmote" && !Context.IsMainPlayer)
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

    internal static bool PlayerInteractWithTable(Table table, Farmer who)
    {
        return Cafe.InteractWithTable(table, who);
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
        Cafe.Menu.SetItems(cafeData.MenuItemLists);
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
            MenuItemLists = new SerializableDictionary<MenuCategory, Inventory>(Cafe.Menu.ItemDictionary)
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
            reset: () => this.LoadedConfig = new ConfigModel(),
            save: () => helper.WriteConfig(this.LoadedConfig)
        );
    }
}
