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
using MyCafe.Characters.Factory;
using MyCafe.Data;
using MyCafe.Data.Customers;
using MyCafe.Data.Models;
using MyCafe.Data.Models.Appearances;
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
using StardewValley.GameData.Buildings;
using StardewValley.Inventories;
using StardewValley.Locations;
using xTile;

namespace MyCafe;

public class Mod : StardewModdingAPI.Mod
{
    internal static Mod Instance = null!;

    private Cafe _cafe = null!;

    private Texture2D _sprites = null!;
    private ConfigModel _loadedConfig = null!;

    private CharacterFactory _characterFactory = null!;

    internal bool IsPlacingSignBoard;

    internal Dictionary<string, VillagerCustomerModel> VillagerCustomerModels = [];

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

    public Mod() => Instance = this;

    /// <inheritdoc/>
    public override void Entry(IModHelper helper)
    {
        Log.Monitor = this.Monitor;
        I18n.Init(helper.Translation);
        this._loadedConfig = helper.ReadConfig<ConfigModel>();
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

        events.Content.AssetRequested += this.OnAssetRequested;
        events.Content.AssetReady += this.OnAssetReady;
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

        this.LoadContent(this.Helper.ContentPacks.CreateTemporary(
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
        LoadCafeData();
        Pathfinding.AddRoutesToFarm();
        ModUtility.CleanUpCustomers();
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

        SaveCafeData();
    }

    internal void OnDayEnding(object? sender, DayEndingEventArgs e)
    {
        if (!Context.IsMainPlayer)
            return;

        // Delete customers
        Cafe.RemoveAllCustomers();
        ModUtility.CleanUpCustomers();
    }

    internal void OnTimeChanged(object? sender, TimeChangedEventArgs e)
    {
        if (!Context.IsMainPlayer)
            return;
        Cafe.TenMinuteUpdate();
    }

    [SuppressMessage("ReSharper", "PossibleLossOfFraction")]
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
                            if (Debug.IsDebug() || Game1.timeOfDay < Cafe.OpeningTime)
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
            foreach (Vector2 tile in TileHelper.GetCircularTileGrid(
                         new Vector2((Game1.viewport.X + Game1.getOldMouseX(false)) / 64,
                             (Game1.viewport.Y + Game1.getOldMouseY(false)) / 64), this._loadedConfig.DistanceForSignboardToRegisterTables))
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

    internal static void LoadCafeData()
    {
        // Load cafe data
        if (!Game1.IsMasterGame || string.IsNullOrEmpty(Constants.CurrentSavePath) || !File.Exists(Path.Combine(Constants.CurrentSavePath, "MyCafe", "cafedata")))
            return;

        string cafeDataFile = Path.Combine(Constants.CurrentSavePath, "MyCafe", "cafedata");

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
            if (!Instance.VillagerCustomerModels.TryGetValue(data.NpcName, out VillagerCustomerModel? model))
            {
                Log.Debug("Loading NPC customer data but not model found. Skipping...");
                continue;
            }

            Log.Trace($"Loading customer data from save file {model.NpcName}");
            data.NpcName = model.NpcName;
            Cafe.VillagerData[data.NpcName] = data;
        }
    }

    internal static void SaveCafeData()
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
            VillagerCustomersData = Cafe.VillagerData.Values.ToList()
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

    

    internal void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
    {
        // NPC Schedules
        if (e.NameWithoutLocale.IsEquivalentTo(ModKeys.ASSETS_NPCSCHEDULE))
        {
            Dictionary<string, VillagerCustomerModel> data = [];

            DirectoryInfo schedulesFolder = new DirectoryInfo(Path.Combine(this.Helper.DirectoryPath, "assets", "VillagerSchedules"));
            foreach (FileInfo file in schedulesFolder.GetFiles())
            {
                VillagerCustomerModel model = this.Helper.ModContent.Load<VillagerCustomerModel>(file.FullName);
                string npcName = file.Name.Replace(".json", "");
                model.NpcName = npcName;
                data[npcName] = model;
            }

            e.LoadFrom(() => data, AssetLoadPriority.Medium);
        }

        // Buildings data (Cafe and signboard)
        else if (e.NameWithoutLocale.IsEquivalentTo("Data/Buildings"))
        {
            e.Edit(asset =>
            {
                var data = asset.AsDictionary<string, BuildingData>();
                data.Data[ModKeys.CAFE_BUILDING_BUILDING_ID] = this.Helper.ModContent.Load<BuildingData>(Path.Combine("assets", "Buildings", "Cafe", "cafebuilding.json"));
                data.Data[ModKeys.CAFE_SIGNBOARD_BUILDING_ID] = this.Helper.ModContent.Load<BuildingData>(Path.Combine("assets", "Buildings", "Signboard", "signboard.json"));
            }, AssetEditPriority.Early);
        }

        // Cafe building texture
        else if (e.NameWithoutLocale.IsEquivalentTo(ModKeys.CAFE_BUILDING_TEXTURE_NAME))
        {
            e.LoadFromModFile<Texture2D>(Path.Combine("assets", "Buildings", "Cafe", "cafebuilding.png"), AssetLoadPriority.Low);
        }

        // Cafe map tmx
        else if (e.NameWithoutLocale.IsEquivalentTo($"Maps/{ModKeys.CAFE_MAP_NAME}"))
        {
            e.LoadFromModFile<Map>(Path.Combine("assets", "Buildings", "Cafe", "cafemap.tmx"), AssetLoadPriority.Low);
        }

        // Signboard building texture
        else if (e.NameWithoutLocale.IsEquivalentTo(ModKeys.CAFE_SIGNBOARD_TEXTURE_NAME))
        {
            e.LoadFromModFile<Texture2D>(Path.Combine("assets", "Buildings", "Signboard", "signboard.png"), AssetLoadPriority.Low);
        }

        // Random generated sprite (with a GUID after the initial asset name)
        else if (e.NameWithoutLocale.StartsWith(ModKeys.GENERATED_SPRITE_PREFIX))
        {
            string id = e.NameWithoutLocale.Name[(ModKeys.GENERATED_SPRITE_PREFIX.Length + 1)..];
            bool failed = false;

            if (Mod.Cafe.GeneratedSprites.TryGetValue(id, out GeneratedSpriteData data))
            {
                Texture2D? sprite = data.Sprite;
                if (sprite == null)
                {
                    Log.Error("Couldn't load texture from generated sprite data!");
                    failed = true;
                }
                else
                    e.LoadFrom(() => sprite, AssetLoadPriority.Medium);
            }
            else
            {
                Log.Error($"Couldn't find generate sprite data for guid {id}");
                failed = true;
            }

            if (failed)
            {
                // Either provide premade error texture or just load null and let the NPC.draw method handle it
                //e.LoadFrom(() => Game1.content.Load<Texture2D>(FarmAnimal.ErrorTextureName), AssetLoadPriority.Medium);
                e.LoadFrom(() => null!, AssetLoadPriority.Medium);
            }
        }

        // Custom events (Added by CP component)
        else if (e.NameWithoutLocale.IsEquivalentTo($"Data/{ModKeys.MODASSET_EVENTS}"))
        {
            e.LoadFrom(() => new Dictionary<string, string>(), AssetLoadPriority.Low);
        }

        // Cafe introduction event (Inject into Data/Events/<cafelocation> only when we need to, the game triggers the event and the event sets a mail flag that disables this
        else if (e.NameWithoutLocale.IsDirectlyUnderPath("Data/Events") &&
                 Context.IsMainPlayer &&
                 Game1.MasterPlayer.mailReceived.Contains(ModKeys.MAILFLAG_HAS_BUILT_SIGNBOARD) &&
                 Game1.MasterPlayer.mailReceived.Contains(ModKeys.MAILFLAG_HAS_SEEN_CAFE_INTRODUCTION) == false)
        {
            GameLocation eventLocation = Game1.locations.First(l => l.isBuildingConstructed(ModKeys.CAFE_SIGNBOARD_BUILDING_ID));
            if (e.NameWithoutLocale.IsEquivalentTo($"Data/Events/{eventLocation.Name}"))
            {
                string @event = Mod.GetCafeIntroductionEvent();
                e.Edit((asset) =>
                {
                    IDictionary<string, string> data = asset.AsDictionary<string, string>().Data;
                    data[$"{ModKeys.MODASSET_EVENTS.Replace('/', '_')}_{ModKeys.EVENT_CAFEINTRODUCTION}/M 100"] = @event;
                });
            }
        }
    }

    internal void OnAssetReady(object? sender, AssetReadyEventArgs e)
    {
        // NPC Schedules
        if (e.NameWithoutLocale.IsEquivalentTo(ModKeys.ASSETS_NPCSCHEDULE))
        {
            this.VillagerCustomerModels = Game1.content.Load<Dictionary<string, VillagerCustomerModel>>(ModKeys.ASSETS_NPCSCHEDULE);
        }
    }

    internal void LoadContent(IContentPack defaultContent)
    {
        this.VillagerCustomerModels = Game1.content.Load<Dictionary<string, VillagerCustomerModel>>(ModKeys.ASSETS_NPCSCHEDULE);

        // Load default content pack included in assets folder
        this.LoadContentPack(defaultContent);

        // Load content packs
        foreach (IContentPack contentPack in this.Helper.ContentPacks.GetOwned())
            this.LoadContentPack(contentPack);

        Mod.CharacterFactory.BodyBase = this.Helper.ModContent.Load<IRawTextureData>(Path.Combine("assets", "CharGen", "base.png"));
        Mod.CharacterFactory.Eyes = this.Helper.ModContent.Load<IRawTextureData>(Path.Combine("assets", "CharGen", "eyes.png"));
        Mod.CharacterFactory.SkinTones = this.Helper.ModContent.Load<List<int[]>>(Path.Combine("assets", "CharGen", "skintones.json"));
        Mod.CharacterFactory.EyeColors = this.Helper.ModContent.Load<List<int[]>>(Path.Combine("assets", "CharGen", "eyecolors.json"));
        Mod.CharacterFactory.HairColors = this.Helper.ModContent.Load<List<AppearancePaint>>(Path.Combine("assets", "CharGen", "haircolors.json"));
        Mod.CharacterFactory.ShirtColors = this.Helper.ModContent.Load<List<AppearancePaint>>(Path.Combine("assets", "CharGen", "shirtcolors.json"));
        Mod.CharacterFactory.PantsColors = this.Helper.ModContent.Load<List<AppearancePaint>>(Path.Combine("assets", "CharGen", "pantscolors.json"));
    }

    internal void LoadContentPack(IContentPack contentPack)
    {
        Log.Info($"Loading content pack {contentPack.Manifest.Name} by {contentPack.Manifest.Author}");

        DirectoryInfo customersFolder = new(Path.Combine(contentPack.DirectoryPath, "Customers"));
        DirectoryInfo hairsFolder = new(Path.Combine(contentPack.DirectoryPath, "Hairstyles"));
        DirectoryInfo shirtsFolder = new(Path.Combine(contentPack.DirectoryPath, "Shirts"));
        DirectoryInfo pantsFolder = new(Path.Combine(contentPack.DirectoryPath, "Pants"));
        DirectoryInfo shoesFolder = new(Path.Combine(contentPack.DirectoryPath, "Shoes"));
        DirectoryInfo accessoriesFolder = new(Path.Combine(contentPack.DirectoryPath, "Accessories"));
        DirectoryInfo outfitsFolder = new(Path.Combine(contentPack.DirectoryPath, "Outfits"));

        if (customersFolder.Exists)
        {
            DirectoryInfo[] customerModels = customersFolder.GetDirectories("*", SearchOption.TopDirectoryOnly);

            // Load customer models
            foreach (DirectoryInfo modelFolder in customerModels)
            {
                string relativePathOfModel = Path.Combine("Customers", modelFolder.Name);
                CustomerModel? model = contentPack.ReadJsonFile<CustomerModel>(Path.Combine(relativePathOfModel, "customer.json"));
                if (model == null)
                {
                    Log.Debug("Couldn't read customer.json for content pack");
                    continue;
                }
                model.Name = modelFolder.Name;
                model.Spritesheet = contentPack.ModContent.GetInternalAssetName(Path.Combine(relativePathOfModel, "customer.png")).Name;

                string portraitPath = Path.Combine(relativePathOfModel, "portrait.png");

                model.Portrait = contentPack.HasFile(portraitPath)
                    ? contentPack.ModContent.GetInternalAssetName(portraitPath).Name
                    : this.Helper.ModContent.GetInternalAssetName(Path.Combine("assets", "CharGen", "Portraits", (string.IsNullOrEmpty(model.Portrait) ? "cat" : model.Portrait) + ".png")).Name;

                Log.Trace($"Customer model added: {model.Name}");
                Mod.CharacterFactory.Customers[$"{contentPack.Manifest.UniqueID}/{model.Name}"] = model;
            }
        }

        if (hairsFolder.Exists)
        {
            DirectoryInfo[] hairModels = hairsFolder.GetDirectories("*", SearchOption.TopDirectoryOnly);
            foreach (DirectoryInfo modelFolder in hairModels)
            {
                HairModel? model = LoadAppearanceModel<HairModel>(contentPack, modelFolder.Name);
                if (model != null) 
                    Mod.CharacterFactory.Hairstyles[model.Id] = model;
            }
        }

        if (shirtsFolder.Exists)
        {
            DirectoryInfo[] shirtModels = shirtsFolder.GetDirectories("*", SearchOption.TopDirectoryOnly);
            foreach (DirectoryInfo modelFolder in shirtModels)
            {
                ShirtModel? model = LoadAppearanceModel<ShirtModel>(contentPack, modelFolder.Name);
                if (model != null) 
                    Mod.CharacterFactory.Shirts[model.Id] = model;
            }
        }

        if (pantsFolder.Exists)
        {
            DirectoryInfo[] pantsModels = pantsFolder.GetDirectories("*", SearchOption.TopDirectoryOnly);
            foreach (DirectoryInfo modelFolder in pantsModels)
            {
                PantsModel? model = LoadAppearanceModel<PantsModel>(contentPack, modelFolder.Name);
                if (model != null) 
                    Mod.CharacterFactory.Pants[model.Id] = model;
            }
        }

        if (shoesFolder.Exists)
        {
            DirectoryInfo[] shoesModels = shoesFolder.GetDirectories("*", SearchOption.TopDirectoryOnly);
            foreach (DirectoryInfo modelFolder in shoesModels)
            {
                ShoesModel? model = LoadAppearanceModel<ShoesModel>(contentPack, modelFolder.Name);
                if (model != null) 
                    Mod.CharacterFactory.Shoes[model.Id] = model;
            }
        }

        if (accessoriesFolder.Exists)
        {
            DirectoryInfo[] accessoryModels = accessoriesFolder.GetDirectories("*", SearchOption.TopDirectoryOnly);
            foreach (DirectoryInfo modelFolder in accessoryModels)
            {
                AccessoryModel? model = LoadAppearanceModel<AccessoryModel>(contentPack, modelFolder.Name);
                if (model != null) 
                    Mod.CharacterFactory.Accessories[model.Id] = model;
            }
        }

        // Load outfits
        if (outfitsFolder.Exists)
        {
            DirectoryInfo[] outfitModels = outfitsFolder.GetDirectories("*", SearchOption.TopDirectoryOnly);
            foreach (DirectoryInfo modelFolder in outfitModels)
            {
                OutfitModel? model = LoadAppearanceModel<OutfitModel>(contentPack, modelFolder.Name);
                if (model != null) 
                    Mod.CharacterFactory.Outfits[model.Id] = model;
            }
        }
    }

    internal static TAppearance? LoadAppearanceModel<TAppearance>(IContentPack contentPack, string modelName) where TAppearance : AppearanceModel
    {
        string filename = ModUtility.GetFileNameForAppearanceType<TAppearance>();
        string folderName = ModUtility.GetFolderNameForAppearance<TAppearance>();

        string relativePathOfModel = Path.Combine(folderName, modelName);
        TAppearance? model = contentPack.ReadJsonFile<TAppearance>(Path.Combine(relativePathOfModel, $"{filename}.json"));
        if (model == null)
        {
            Log.Debug($"Couldn't read {filename}.json for content pack {contentPack.Manifest.UniqueID}");
            return null;
        }

        model.Id = $"{contentPack.Manifest.UniqueID}/{modelName}";
        model.TexturePath = contentPack.ModContent.GetInternalAssetName(Path.Combine(relativePathOfModel, $"{filename}.png")).Name;
        model.ContentPack = contentPack;

        Log.Trace($"{folderName} model added: {model.Id}");

        return model;
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
