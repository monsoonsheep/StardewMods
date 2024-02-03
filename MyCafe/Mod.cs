using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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
    internal static string UniqueId = null!;

    internal ISpaceCoreApi SpaceCore = null!;

    internal ConfigModel LoadedConfig = null!;
    internal static Texture2D Sprites = null!;
    internal Dictionary<string, BusCustomerData> CustomersData = [];

    internal static bool IsPlacingSignBoard;

    internal NetRef<Cafe> NetCafe = [];

    internal static Cafe Cafe
        => Instance.NetCafe.Value;

    internal static GameLocation? CafeIndoor => Cafe.Indoor;
    internal static GameLocation? CafeOutdoor => Cafe.Outdoor;

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

        // Harmony patches
        if (HarmonyPatcher.TryApply(this,
                new ActionPatcher(),
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

        events.Multiplayer.ModMessageReceived += this.OnModMessageReceived;
        events.Content.AssetRequested += this.OnAssetRequested;
        events.Content.AssetReady += OnAssetReady;
        events.Display.RenderedWorld += this.OnRenderedWorld;
        events.World.FurnitureListChanged += this.OnFurnitureListChanged;
        events.Input.ButtonPressed += Debug.ButtonPress;

        Sprites = helper.ModContent.Load<Texture2D>(Path.Combine("assets", "sprites.png"));
    }

    private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
    {
        this.InitializeGmcm(this.Helper, this.ModManifest);
        this.SpaceCore = this.Helper.ModRegistry.GetApi<ISpaceCoreApi>("spacechase0.SpaceCore")!;
        this.SpaceCore.RegisterSerializerType(typeof(CafeLocation));
        CafeState.Register();
        GameLocation.RegisterTileAction(ModKeys.SIGNBOARD_BUILDING_CLICK_EVENT_KEY, CafeMenu.Action_OpenCafeMenu);
    }

    private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
    {
        if (!Context.IsMainPlayer)
            return;

        this.LoadContentPacks();
        this.LoadCafeData();
        Cafe.Initialize(this.Helper, this.CustomersData);
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
    }

    internal void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
    {
        // NPC Schedules
        if (e.Name.IsDirectlyUnderPath(ModKeys.ASSETS_NPCSCHEDULE_PREFIX))
        {
            string npcname = e.Name.Name.Split('/').Last();
            VillagerCustomerData? data = this.Helper.Data.ReadJsonFile<VillagerCustomerData>(Path.Combine("assets", "Schedules", npcname + ".json"));
            if (data != null)
                e.LoadFrom(() => data, AssetLoadPriority.Low);
        }

        // Buildings data (Cafe and signboard)
        else if (e.Name.IsEquivalentTo("Data/Buildings"))
        {
            e.Edit(asset =>
            {
                IAssetDataForDictionary<string, BuildingData> data = asset.AsDictionary<string, BuildingData>();

                data.Data[ModKeys.CAFE_BUILDING_BUILDING_ID] = this.Helper.ModContent.Load<BuildingData>(Path.Combine("assets", "Buildings", "Cafe", "cafebuilding.json"));
                data.Data[ModKeys.CAFE_SIGNBOARD_BUILDING_ID] = this.Helper.ModContent.Load<BuildingData>(Path.Combine("assets", "Buildings", "Signboard", "signboard.json"));
            }, AssetEditPriority.Early);
        }

        // Cafe building texture
        else if (e.Name.IsEquivalentTo(ModKeys.CAFE_BUILDING_TEXTURE_NAME))
        {
            e.LoadFromModFile<Texture2D>(Path.Combine("assets", "Buildings", "Cafe", "cafebuilding.png"), AssetLoadPriority.Low);
        }
        // Cafe map tmx
        else if (e.Name.IsEquivalentTo($"Maps/{ModKeys.CAFE_MAP_NAME}"))
        {
            e.LoadFromModFile<Map>(Path.Combine("assets", "Buildings", "Cafe", "cafemap.tmx"), AssetLoadPriority.Low);
        }
        // Signboard building texture
        else if (e.Name.IsEquivalentTo(ModKeys.CAFE_SIGNBOARD_TEXTURE_NAME))
        {
            e.LoadFromModFile<Texture2D>(Path.Combine("assets", "Buildings", "Signboard", "signboard.png"), AssetLoadPriority.Low);
        }
    }

    internal static void OnAssetReady(object? sender, AssetReadyEventArgs e)
    {
        if (e.Name.IsDirectlyUnderPath(ModKeys.ASSETS_NPCSCHEDULE_PREFIX))
        {
            string npcname = e.Name.Name.Split('/').Last();
        }
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
                Log.Debug("Invalid message from host", LogLevel.Error);
            }
        }
    }

    internal void LoadContentPacks()
    {
        this.CustomersData = [];

        foreach (IContentPack contentPack in this.Helper.ContentPacks.GetOwned())
        {
            Log.Debug($"Loading content pack {contentPack.Manifest.Name} by {contentPack.Manifest.Author}");
            var modelsInPack = new DirectoryInfo(Path.Combine(contentPack.DirectoryPath, "Customers")).GetDirectories();
            foreach (var modelFolder in modelsInPack)
            {
                CustomerModel? model = contentPack.ReadJsonFile<CustomerModel>(Path.Combine("Customers", modelFolder.Name, "customer.json"));
                if (model == null)
                {
                    Log.Debug("Couldn't read json for content pack");
                    continue;
                }

                model.Name = model.Name.Replace(" ", "");
                model.Spritesheet = contentPack.ModContent.GetInternalAssetName(Path.Combine("Customers", modelFolder.Name, "sprite.png")).Name;

                if (contentPack.HasFile(Path.Combine("Customers", modelFolder.Name, "portrait.png")))
                {
                    model.PortraitName = contentPack.ModContent.GetInternalAssetName(Path.Combine("Customers", modelFolder.Name, "portrait.png")).Name;
                }
                else
                {
                    string portraitName = string.IsNullOrEmpty(model.PortraitName) ? "cat" : model.PortraitName;
                    model.PortraitName = this.Helper.ModContent.GetInternalAssetName(Path.Combine("assets", "Portraits", portraitName + ".png")).Name;
                }

                this.CustomersData[model.Name] = new BusCustomerData()
                {
                    Model = model
                };
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

public static class CafeState
{
    internal class Holder
    {
        public NetRef<Cafe> Value = new(new Cafe());
    }

    internal static ConditionalWeakTable<Farm, Holder> Values = new();

    internal static void Register()
    {
        Mod.Instance.SpaceCore.RegisterCustomProperty(
            typeof(Farm),
            "Cafe",
            typeof(NetRef<Cafe>),
            AccessTools.Method(typeof(CafeState), nameof(get_Cafe)),
            AccessTools.Method(typeof(CafeState), nameof(set_Cafe)));
    }

    public static NetRef<Cafe> get_Cafe(this Farm farm)
    {
        Holder holder = Values.GetOrCreateValue(farm);
        return holder.Value;
    }

    public static void set_Cafe(this Farm farm, NetRef<Cafe> value)
    {
        Log.Error("Setting Cafe field for Farm. Should this be happening?");
        Holder holder = Values.GetOrCreateValue(farm);
        holder.Value = value;
    }
}
