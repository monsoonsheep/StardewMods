using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Serialization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
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
using SpaceCore.Events;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.Delegates;
using StardewValley.GameData.Buildings;
using StardewValley.GameData.Pets;
using StardewValley.GameData.Tools;
using StardewValley.Inventories;
using StardewValley.Locations;
using StardewValley.TokenizableStrings;
using xTile;
using Object = StardewValley.Object;

namespace MyCafe;

public class Mod : StardewModdingAPI.Mod
{
    internal static Mod Instance = null!;

    private Cafe _cafe = null!;

    private Texture2D _sprites = null!;

    private ConfigModel _loadedConfig = null!;

    private RandomCharacterGenerator _randomCharacterGenerator = null!;

    internal Dictionary<string, VillagerCustomerModel> VillagerCustomerModels = [];

    internal Dictionary<string, VillagerCustomerData> VillagerData = [];

    internal Dictionary<string, CustomerModel> CustomerModels = [];

    internal Dictionary<string, CustomerData> CustomerData = [];

    internal static string UniqueId
        => Instance.ModManifest.UniqueID;

    internal static Cafe Cafe
        => Instance._cafe;

    internal static Texture2D Sprites
        => Instance._sprites;

    internal static ConfigModel Config
        => Instance._loadedConfig;

    internal static RandomCharacterGenerator RandomCharacterGenerator
        => Instance._randomCharacterGenerator;

    public Mod() => Instance = this;

    /// <inheritdoc/>
    public override void Entry(IModHelper helper)
    {
        // Why do I keep doing this
        Log.Monitor = this.Monitor;
        I18n.Init(this.Helper.Translation);
        this._loadedConfig = this.Helper.ReadConfig<ConfigModel>();
        this._cafe = new Cafe();
        this._randomCharacterGenerator = new RandomCharacterGenerator(this.Helper);

        // Harmony patches
        if (HarmonyPatcher.TryApply(this,
                new ActionPatcher(),
                new LocationPatcher(),
                new NetFieldPatcher(),
                new CharacterPatcher(),
                new FurniturePatcher(),
                new DialoguePatcher()
            ) is false)
            return;

        IModEvents events = this.Helper.Events;

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

        #if DEBUG
        events.Input.ButtonPressed += Debug.ButtonPress;
        #endif
    }

    private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
    {
        this._sprites = Game1.content.Load<Texture2D>(ModKeys.MODASSET_SPRITES);

        this.InitializeGmcm(this.Helper, this.ModManifest);

        GameLocation.RegisterTileAction(ModKeys.SIGNBOARD_BUILDING_CLICK_EVENT_KEY, CafeMenu.Action_OpenCafeMenu);

        SpaceEvents.OnEventFinished += this.OnEventFinished;
        //ISpaceCoreApi spaceCore = this.Helper.ModRegistry.GetApi<ISpaceCoreApi>("spacechase0.SpaceCore")!;
        // Remove this? It might not be need. Test multiplayer to confirm.
        //FarmerTeamVirtualProperties.Register(spaceCore);

        this.LoadContent(this.Helper.ContentPacks.CreateTemporary(
            Path.Combine(this.Helper.DirectoryPath, "assets", "DefaultContent"),
            $"{this.ModManifest.Author}.DefaultContent",
            "MyCafe Fake Content Pack",
            "Default content for MyCafe",
            this.ModManifest.Author,
            this.ModManifest.Version
            ));

        TokenParser.RegisterParser(
            ModKeys.TOKEN_RANDOM_MENU_ITEM,
            (string[] query, out string replacement, Random random, Farmer player) =>
            {
                replacement = Mod.Cafe.Menu.ItemDictionary.Values.SelectMany(i => i).ToList().PickRandom()?.DisplayName ?? "Special";
                return true;
            });

        GameStateQuery.Register(
            ModKeys.GAMESTATEQUERY_ISINDOORCAFE,
            (query, context) => !Game1.currentLocation.IsOutdoors);

        this.Helper.ConsoleCommands.Add("cafe_givesignboard", "Gives you the cafe signboard (if you want to skip the Gus 7-heart event", this.GiveSignboard);
    }

    private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
    {
        if (!Context.IsMainPlayer)
            return;

        Cafe.InitializeForHost(this.Helper);
        this.LoadCafeData();

        foreach (var model in this.VillagerCustomerModels)
            if (!this.VillagerData.ContainsKey(model.Key))
                this.VillagerData[model.Key] = new VillagerCustomerData(model.Key);

         
        foreach (var model in this.CustomerModels)
            if (!this.CustomerData.ContainsKey(model.Key))
                this.CustomerData[model.Key] = new CustomerData(model.Key);
        
        Pathfinding.AddRoutesToFarm();
        ModUtility.CleanUpCustomers();
    }

    internal void OnDayStarted(object? sender, DayStartedEventArgs e)
    {
        if (!Context.IsMainPlayer)
            return;

        Cafe.UpdateSpouse();
        Cafe.UpdateLocations();
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
        if (Cafe.Enabled == 0)
            return;

        // Get list of reserved tables with center coords
        foreach (Table table in Cafe.Tables)
        {
            if (Game1.currentLocation.Name.Equals(table.Location))
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
                        // Display number of seats on table
                        if (Cafe.Enabled == 1)
                        {
                            e.SpriteBatch.DrawString(Game1.tinyFont, table.Seats.Count.ToString(), Game1.GlobalToLocal(table.Center + new Vector2(-12, -112)) + offset, Color.LightBlue, 0f, Vector2.Zero, 5f, SpriteEffects.None, 0.99f);
                            e.SpriteBatch.DrawString(Game1.tinyFont, table.Seats.Count.ToString(), Game1.GlobalToLocal(table.Center + new Vector2(-10, -96)) + offset, Color.Black, 0f, Vector2.Zero, 4f, SpriteEffects.None, 1f);
                        }

                        break;
                }
            }
        }

        foreach (NPC c in Cafe.Groups.SelectMany(g => g.Members))
        {
            float layerDepth = Math.Max(0f, c.StandingPixel.Y / 10000f);
            Vector2 drawPosition = c.getLocalPosition(Game1.viewport);

            if (c.get_DrawName().Value == true)
            {
                e.SpriteBatch.DrawString(
                    Game1.dialogueFont,
                    c.displayName,
                    drawPosition - new Vector2(40, 64),
                    Color.White * 0.75f,
                    0f,
                    Vector2.Zero,
                    new Vector2(0.3f, 0.3f),
                    SpriteEffects.None,
                    layerDepth + 0.001f
                );
            }

            Item item;
            if (c.get_DrawOrderItem().Value == true && (item = c.get_OrderItem().Value) != null)
            {
                Vector2 offset = new Vector2(0,
                    (float) Math.Round(4f * Math.Sin(Game1.currentGameTime.TotalGameTime.TotalMilliseconds / 250.0)));

                drawPosition.Y -= 32 + c.Sprite.SpriteHeight * 3;

                // Draw bubble
                e.SpriteBatch.Draw(
                    Sprites,
                    drawPosition + offset,
                    new Rectangle(0, 16, 16, 16),
                    Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None,
                    0.99f);

                // Item inside the bubble
                item.drawInMenu(e.SpriteBatch, drawPosition + offset, 0.40f, 1f, 0.992f, StackDrawType.Hide, Color.White, drawShadow: false);

                // Draw item name if hovering over bubble
                Vector2 mouse = new Vector2(Game1.getMouseX(), Game1.getMouseY());
                if (Vector2.Distance(drawPosition, mouse) <= Game1.tileSize)
                {
                    Vector2 size = Game1.dialogueFont.MeasureString(item.DisplayName) * 0.75f;
                    e.SpriteBatch.DrawString(Game1.dialogueFont, item.DisplayName, drawPosition + new Vector2(32 - size.X / 2f, -10f), Color.Black, 0f, Vector2.Zero, 0.75f, SpriteEffects.None, 1f);
                }
            }
        }
    }

    internal void OnFurnitureListChanged(object? sender, FurnitureListChangedEventArgs e)
    {
        if (!Context.IsMainPlayer || Cafe.Enabled == 0)
            return;

        if (e.Location.Equals(Cafe.Signboard?.Location))
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

    private void OnEventFinished(object? sender, EventArgs e)
    {
        if (Game1.CurrentEvent.id.Equals(ModKeys.EVENT_CAFEINTRODUCTION) && !Game1.player.Items.ContainsId(ModKeys.CAFE_SIGNBOARD_OBJECT_ID))
        {
            Game1.player.addItemByMenuIfNecessaryElseHoldUp(ItemRegistry.Create<Object>($"(BC){ModKeys.CAFE_SIGNBOARD_OBJECT_ID}"));
        }
    }

    internal void LoadCafeData()
    {
        // Load cafe data
        if (!Game1.IsMasterGame || string.IsNullOrEmpty(Constants.CurrentSavePath) || !File.Exists(Path.Combine(Constants.CurrentSavePath, "MyCafe", "cafedata")))
            return;

        string cafeDataFile = Path.Combine(Constants.CurrentSavePath, "MyCafe", "cafedata");

        CafeArchiveData loaded;
        XmlSerializer serializer = new XmlSerializer(typeof(CafeArchiveData));

        try
        {
            using StreamReader reader = new StreamReader(cafeDataFile);
            loaded = (CafeArchiveData) serializer.Deserialize(reader)!;
        }
        catch (InvalidOperationException e)
        {
            Log.Error("Couldn't read Cafe Data from external save folder");
            Log.Error($"{e.Message}\n{e.StackTrace}");
            return;
        }

        Cafe.OpeningTime = loaded.OpeningTime;
        Cafe.ClosingTime = loaded.ClosingTime;
        Cafe.Menu.MenuObject.Set(loaded.MenuItemLists);

        foreach (var loadedEntry in loaded.VillagerCustomersData)
        {
            if (!this.VillagerCustomerModels.TryGetValue(loadedEntry.Key, out VillagerCustomerModel? model))
            {
                Log.Debug("Loading NPC customer data but not model found. Skipping...");
                continue;
            }

            Log.Trace($"Loading customer data from save file {model.NpcName}");
            loadedEntry.Value.NpcName = model.NpcName;
            this.VillagerData[loadedEntry.Key] = loadedEntry.Value;
        }

        foreach (var loadedEntry in loaded.CustomersData)
        {
            if (!this.CustomerModels.TryGetValue(loadedEntry.Key, out CustomerModel? model))
            {
                Log.Debug("Loading custom customer data but not model found. Skipping...");
                continue;
            }

            Log.Trace($"Loading custom customer data from save file {model.Name}");
            loadedEntry.Value.Id = model.Name;
            this.CustomerData[loadedEntry.Key] = loadedEntry.Value;
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
            VillagerCustomersData = new SerializableDictionary<string, VillagerCustomerData>(this.VillagerData),
            CustomersData = new SerializableDictionary<string, CustomerData>(this.CustomerData)
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
            text: () => "Customers"
            );

        configMenu.AddBoolOption(
            mod: manifest,
            getValue: () => this._loadedConfig.WarpCustomers,
            setValue: (value) => this._loadedConfig.WarpCustomers = value,
            name: () => "Warp Customers",
            tooltip: () => "Teleport customers through unloaded locations to save time for their schedules and speed up visits");

        configMenu.AddNumberOption(
            mod: manifest,
            getValue: () => this._loadedConfig.MinutesBeforeCustomersLeave,
            setValue: (value) => this._loadedConfig.MinutesBeforeCustomersLeave = value,
            name: () => "Minutes Before Customers Leave",
            tooltip: () => "How many minutes (game time) customers wait for their order before they leave",
            min: 20,
            max: 720,
            interval: 10,
            formatValue: (value) => value + "m"
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
            formatValue: (value) => value == 0 ? "Disabled" : value.ToString()
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
            formatValue: (value) => value == 0 ? "Disabled" : value.ToString()
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
            formatValue: (value) => value == 0 ? "Disabled" : value.ToString()
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
            formatValue: (value) => value + " tiles"
            );
    }

    internal void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
    {
        // Random generated sprite (with a GUID after the initial asset name)
        if (e.NameWithoutLocale.StartsWith(ModKeys.GENERATED_SPRITE_PREFIX))
        {
            if (Cafe.GeneratedSprites.TryGetValue(e.NameWithoutLocale.Name[(ModKeys.GENERATED_SPRITE_PREFIX.Length + 1)..], out GeneratedSpriteData data))
            {
                e.LoadFrom(() => data.Sprite.Value, AssetLoadPriority.Medium);
            }
            else
            {
                Log.Error($"Couldn't find generate sprite data for guid {e.NameWithoutLocale.Name}");
                // Either provide premade error texture or just load null and let the NPC.draw method handle it TODO test the error
                //e.LoadFrom(() => Game1.content.Load<Texture2D>(FarmAnimal.ErrorTextureName), AssetLoadPriority.Medium);
                e.LoadFrom(() => null!, AssetLoadPriority.Medium);
            }
        }
        
        // Injecting cafe introduction event (Inject into Data/Events/<cafelocation> only when we need to, the game triggers the event and the event sets a mail flag that disables this
        else if (e.NameWithoutLocale.IsDirectlyUnderPath("Data/Events") &&
                 Context.IsMainPlayer &&
                 Game1.MasterPlayer.mailReceived.Contains(ModKeys.MAILFLAG_HAS_BUILT_SIGNBOARD) &&
                 Game1.MasterPlayer.mailReceived.Contains(ModKeys.MAILFLAG_HAS_SEEN_CAFE_INTRODUCTION) == false)
        {
            GameLocation eventLocation = Game1.getFarm();
            //GameLocation eventLocation = Game1.locations.First(l => l.isBuildingConstructed(ModKeys.CAFE_SIGNBOARD_BUILDING_ID));
            if (e.NameWithoutLocale.IsEquivalentTo($"Data/Events/{eventLocation.Name}"))
            {
                string @event = this.GetCafeIntroductionEvent();

                e.Edit((asset) =>
                {
                    IDictionary<string, string> data = asset.AsDictionary<string, string>().Data;
                    data[$"{ModKeys.EVENT_CAFEINTRODUCTION}/"] = @event;
                });
            }
        }

        // Mod sprites
        else if (e.NameWithoutLocale.IsEquivalentTo(ModKeys.MODASSET_SPRITES))
        {
            e.LoadFromModFile<Texture2D>(Path.Combine("assets", "sprites.png"), AssetLoadPriority.Medium);
        }

        // NPC Schedules
        else if (e.NameWithoutLocale.IsEquivalentTo(ModKeys.MODASSET_NPC_VISITING_DATA))
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
    }

    internal void OnAssetReady(object? sender, AssetReadyEventArgs e)
    {
        // NPC Schedules
        if (e.NameWithoutLocale.IsEquivalentTo(ModKeys.MODASSET_NPC_VISITING_DATA))
        {
            this.VillagerCustomerModels = Game1.content.Load<Dictionary<string, VillagerCustomerModel>>(ModKeys.MODASSET_NPC_VISITING_DATA);
        }
    }

    internal string GetCafeIntroductionEvent()
    {
        return string.Empty;

        //GameLocation eventLocation = Game1.locations.First(l => l.isBuildingConstructed(ModKeys.CAFE_SIGNBOARD_BUILDING_ID));
        //Building signboard = eventLocation.getBuildingByType(ModKeys.CAFE_SIGNBOARD_BUILDING_ID);
        //Point signboardTile = new Point(signboard.tileX.Value, signboard.tileY.Value + signboard.tilesHigh.Value);

        //string eventString = Game1.content.Load<Dictionary<string, string>>($"{ModKeys.MODASSET_EVENTS}")[ModKeys.EVENT_CAFEINTRODUCTION];

        //// Replace the encoded coordinates with the position of the signboard building
        //string substituted = Regex.Replace(
        //    eventString,
        //    @"(6\d{2})\s(6\d{2})",
        //    (m) => $"{(int.Parse(m.Groups[1].Value) - 650 + signboardTile.X)} {(int.Parse(m.Groups[2].Value) - 650 + signboardTile.Y)}");

        //return substituted;
    }

    internal void AddDialoguesOnArrivingAtCafe(NPC npc)
    {
        // Add the first time visit dialogues if their data's Last Visited value is the Spring 1, year 1
        string key = this.VillagerData[npc.Name].LastVisitedDate.TotalDays <= 1
            ? ModKeys.MODASSET_DIALOGUE_ENTRY_CAFEFIRSTTIMEVISIT
            : ModKeys.MODASSET_DIALOGUE_ENTRY_CAFEVISIT;

        KeyValuePair<string, string> entry = ModUtility.GetCustomDialogueAssetOrGeneric(npc, key);

        npc.CurrentDialogue.Push(
            new Dialogue(npc, $"{key}:{entry.Key}", TokenParser.ParseText(entry.Value, Game1.random, null, Game1.player))
            {
                removeOnNextMove = true,
                dontFaceFarmer = true
            }
        );
    }

    internal void TryAddDialogueLastAteComment(VillagerCustomerData npcData, Stack<Dialogue> dialogue)
    {
        KeyValuePair<string, string>? entry = ModUtility.GetCustomDialogueAsset(npcData.GetNpc(), ModKeys.MODASSET_DIALOGUE_ENTRY_LASTATECOMMENT);

        if (entry.HasValue)
        {
            string text = TokenParser.ParseText(string.Format(entry.Value.Value, ItemRegistry.GetData(npcData.LastAteFood)?.DisplayName ?? "thing"), Game1.random, null, Game1.player);
            dialogue.Push(new Dialogue(npcData.GetNpc(), $"{ModKeys.MODASSET_DIALOGUE_ENTRY_LASTATECOMMENT}:{entry.Value.Key}", text));
        }
    }

    internal void GiveSignboard(string command, string[] args)
    {
        if (Game1.player.Items.ContainsId(ModKeys.CAFE_SIGNBOARD_OBJECT_ID))
        {
            Log.Warn("You already have the signboard in your inventory");
            return;
        }

        Object signboard = ItemRegistry.Create<Object>($"(BC){ModKeys.CAFE_SIGNBOARD_OBJECT_ID}");
        Game1.player.addItemToInventory(signboard);
    }

    internal void LoadContent(IContentPack defaultContent)
    {
        this.VillagerCustomerModels = Game1.content.Load<Dictionary<string, VillagerCustomerModel>>(ModKeys.MODASSET_NPC_VISITING_DATA);

        // Load default content pack included in assets folder
        this.LoadContentPack(defaultContent);

        // Load content packs
        foreach (IContentPack contentPack in this.Helper.ContentPacks.GetOwned())
            this.LoadContentPack(contentPack);

        RandomCharacterGenerator.BodyBase = this.Helper.ModContent.Load<IRawTextureData>(Path.Combine("assets", "CharGen", "base.png"));
        RandomCharacterGenerator.Eyes = this.Helper.ModContent.Load<IRawTextureData>(Path.Combine("assets", "CharGen", "eyes.png"));
        RandomCharacterGenerator.SkinTones = this.Helper.ModContent.Load<List<int[]>>(Path.Combine("assets", "CharGen", "skintones.json"));
        RandomCharacterGenerator.EyeColors = this.Helper.ModContent.Load<List<int[]>>(Path.Combine("assets", "CharGen", "eyecolors.json"));
        RandomCharacterGenerator.HairColors = this.Helper.ModContent.Load<List<AppearancePaint>>(Path.Combine("assets", "CharGen", "haircolors.json"));
        RandomCharacterGenerator.ShirtColors = this.Helper.ModContent.Load<List<AppearancePaint>>(Path.Combine("assets", "CharGen", "shirtcolors.json"));
        RandomCharacterGenerator.PantsColors = this.Helper.ModContent.Load<List<AppearancePaint>>(Path.Combine("assets", "CharGen", "pantscolors.json"));
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
                model.Name = $"{contentPack.Manifest.UniqueID}/{model.Name}";
                model.Spritesheet = contentPack.ModContent.GetInternalAssetName(Path.Combine(relativePathOfModel, "customer.png")).Name;

                string portraitPath = Path.Combine(relativePathOfModel, "portrait.png");

                model.Portrait = contentPack.HasFile(portraitPath)
                    ? contentPack.ModContent.GetInternalAssetName(portraitPath).Name
                    : this.Helper.ModContent.GetInternalAssetName(Path.Combine("assets", "CharGen", "Portraits", (string.IsNullOrEmpty(model.Portrait) ? "cat" : model.Portrait) + ".png")).Name;

                Log.Trace($"Customer model added: {model.Name}");
                this.CustomerModels[model.Name] = model;
            }
        }

        if (hairsFolder.Exists)
        {
            DirectoryInfo[] hairModels = hairsFolder.GetDirectories("*", SearchOption.TopDirectoryOnly);
            foreach (DirectoryInfo modelFolder in hairModels)
            {
                HairModel? model = LoadAppearanceModel<HairModel>(contentPack, modelFolder.Name);
                if (model != null) 
                    RandomCharacterGenerator.Hairstyles[model.Id] = model;
            }
        }

        if (shirtsFolder.Exists)
        {
            DirectoryInfo[] shirtModels = shirtsFolder.GetDirectories("*", SearchOption.TopDirectoryOnly);
            foreach (DirectoryInfo modelFolder in shirtModels)
            {
                ShirtModel? model = LoadAppearanceModel<ShirtModel>(contentPack, modelFolder.Name);
                if (model != null) 
                    RandomCharacterGenerator.Shirts[model.Id] = model;
            }
        }

        if (pantsFolder.Exists)
        {
            DirectoryInfo[] pantsModels = pantsFolder.GetDirectories("*", SearchOption.TopDirectoryOnly);
            foreach (DirectoryInfo modelFolder in pantsModels)
            {
                PantsModel? model = LoadAppearanceModel<PantsModel>(contentPack, modelFolder.Name);
                if (model != null) 
                    RandomCharacterGenerator.Pants[model.Id] = model;
            }
        }

        if (shoesFolder.Exists)
        {
            DirectoryInfo[] shoesModels = shoesFolder.GetDirectories("*", SearchOption.TopDirectoryOnly);
            foreach (DirectoryInfo modelFolder in shoesModels)
            {
                ShoesModel? model = LoadAppearanceModel<ShoesModel>(contentPack, modelFolder.Name);
                if (model != null) 
                    RandomCharacterGenerator.Shoes[model.Id] = model;
            }
        }

        if (accessoriesFolder.Exists)
        {
            DirectoryInfo[] accessoryModels = accessoriesFolder.GetDirectories("*", SearchOption.TopDirectoryOnly);
            foreach (DirectoryInfo modelFolder in accessoryModels)
            {
                AccessoryModel? model = LoadAppearanceModel<AccessoryModel>(contentPack, modelFolder.Name);
                if (model != null) 
                    RandomCharacterGenerator.Accessories[model.Id] = model;
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
                    RandomCharacterGenerator.Outfits[model.Id] = model;
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

    internal static IBusSchedulesApi? GetBusSchedulesApi()
    {
        return Instance.Helper.ModRegistry.GetApi<IBusSchedulesApi>("MonsoonSheep.BusSchedules");
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
