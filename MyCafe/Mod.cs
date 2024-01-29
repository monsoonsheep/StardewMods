global using SUtility = StardewValley.Utility;
global using SObject = StardewValley.Object;

using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MyCafe.Customers.Data;
using MyCafe.Interfaces;
using MyCafe.Patching;
using Netcode;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.GameData.Buildings;
using StardewValley.Objects;
using System;
using System.Collections.Generic;
using System.Linq;
using MyCafe.Customers;
using xTile;
using System.IO;
using MyCafe.UI;
using System.Runtime.CompilerServices;
using MonsoonSheep.Stardew.Common.Patching;
using StardewValley.Locations;
using MyCafe.Locations.Objects;

namespace MyCafe;

public class Mod : StardewModdingAPI.Mod
{
    internal new static IMonitor Monitor;
    internal static IModHelper ModHelper;
    internal new static IManifest ModManifest;

    internal static ISpaceCoreApi SpaceCore;
        
    internal static ConfigModel Config;
    internal static Texture2D Sprites;
    internal static Dictionary<string, BusCustomerData> CustomersData;

    private static NetRef<Cafe> _cafe;
    internal static Cafe Cafe
    {
        get
        {
            _cafe ??= Game1.getFarm().get_Cafe();
            return _cafe.Value;
        }
        set => _cafe = new NetRef<Cafe>(value);
    }

    /// <inheritdoc/>
    public override void Entry(IModHelper helper)
    {
        Monitor = base.Monitor;
        ModHelper = helper;
        ModManifest = base.ModManifest;
        Log.Monitor = Monitor;
        I18n.Init(helper.Translation);
        Config = helper.ReadConfig<ConfigModel>();

        // Harmony patches
        if (HarmonyPatcher.TryApply(this,
                new LocationPatcher(),
                new CharacterPatcher(),
                new FurniturePatcher()
            ) is false)
            return;
       
        IModEvents events = helper.Events;

        events.GameLoop.GameLaunched += OnGameLaunched;
        events.GameLoop.SaveLoaded += OnSaveLoaded;
        events.GameLoop.DayStarted += OnDayStarted;
        events.GameLoop.Saving += OnSaving;
        events.Multiplayer.ModMessageReceived += OnModMessageReceived;
        events.Content.AssetRequested += OnAssetRequested;
        events.Content.AssetReady += OnAssetReady;
        events.Display.RenderedWorld += OnRenderedWorld;
        events.Input.ButtonPressed += Debug.ButtonPress;

        events.World.FurnitureListChanged += OnFurnitureListChanged;
        events.GameLoop.TimeChanged += OnTimeChanged;
        events.GameLoop.DayEnding += OnDayEnding;

        Sprites = helper.ModContent.Load<Texture2D>(Path.Combine("assets", "sprites.png"));
    }


    private void OnGameLaunched(object sender, GameLaunchedEventArgs e)
    {
        SpaceCore = ModHelper.ModRegistry.GetApi<ISpaceCoreApi>("spacechase0.SpaceCore")!;
        SpaceCore.RegisterSerializerType(typeof(Cafe));
        SpaceCore.RegisterSerializerType(typeof(CafeLocation));
        CafeState.Register();

        ModConfig.InitializeGmcm();
        GameLocation.RegisterTileAction(ModKeys.SIGNBOARD_BUILDING_CLICK_EVENT_KEY, CafeMenu.Action_OpenCafeMenu);
    }

    private void OnSaveLoaded(object sender, SaveLoadedEventArgs e)
    {
        if (!Context.IsMainPlayer) return;

        LoadContentPacks();
        
        if (Game1.player.modData.TryGetValue(ModKeys.MODDATA_OPENCLOSETIMES, out var openclose))
        {
            var split = openclose.Split('|');
            Cafe.OpeningTime.Set(int.Parse(split[0]));
            Cafe.ClosingTime.Set(int.Parse(split[1]));
        }
        if (Game1.player.modData.TryGetValue(ModKeys.MODDATA_MENUITEMSLIST, out var menuitems))
        {
            Cafe.MenuItems.Clear();
            var split = menuitems.Split('|');
            //foreach (var item in split.Select(id => ItemRegistry.Create<Item>(id)))
            //{
            //    Cafe.AddToMenu(item);
            //}
        }
        Cafe.Initialize(ModHelper);
    }

    internal void OnDayStarted(object sender, DayStartedEventArgs e)
    {
        if (!Context.IsMainPlayer) return;

        if (Cafe.UpdateCafeLocations() is true)
        {
            Cafe.Enabled = true;
            Cafe.PopulateTables();
            Cafe.PopulateRoutesToCafe();
        }
        else if (Cafe.Enabled)
            Cafe.Enabled = false;

        if (Cafe.Enabled)
            Cafe.DayUpdate();

    }

    internal void OnSaving(object sender, SavingEventArgs e)
    {
        if (!Context.IsMainPlayer) return;

        Game1.player.modData[ModKeys.MODDATA_OPENCLOSETIMES] = $"{Cafe.OpeningTime.Value}|{Cafe.ClosingTime.Value}";
        Game1.player.modData[ModKeys.MODDATA_MENUITEMSLIST] = "";
    }

    internal void OnDayEnding(object sender, DayEndingEventArgs e)
    {
        if (!Context.IsMainPlayer) return;

        SUtility.ForEachLocation(delegate(GameLocation loc)
        {
            for (int i = loc.characters.Count - 1; i >= 0; i--)
                if (loc.characters[i] is Customer)
                    loc.characters.RemoveAt(i);
            return true;
        });
    }

    internal static void OnAssetRequested(object sender, AssetRequestedEventArgs e)
    {
        // NPC Schedules
        if (e.Name.IsDirectlyUnderPath(ModKeys.ASSETS_NPCSCHEDULE_PREFIX))
        {
            string npcname = e.Name.Name.Split('/').Last();
            var file = ModHelper.Data.ReadJsonFile<VillagerCustomerData>(Path.Combine("assets", "Schedules", npcname + ".json"));
            if (file != null)
                e.LoadFrom(() => file, AssetLoadPriority.Low);
        }

        // Buildings data (Cafe and signboard)
        else if (e.Name.IsEquivalentTo("Data/Buildings"))
        {
            e.Edit(asset =>
            {
                var data = asset.AsDictionary<string, BuildingData>();
                data.Data[ModKeys.CAFE_BUILDING_BUILDING_ID] = ModHelper.ModContent.Load<BuildingData>(Path.Combine("assets", "Buildings", "Cafe", "cafebuilding.json"));
                data.Data[ModKeys.CAFE_SIGNBOARD_BUILDING_ID] = ModHelper.ModContent.Load<BuildingData>(Path.Combine("assets", "Buildings", "Signboard", "signboard.json"));
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
            e.LoadFromModFile<Texture2D>("assets/Cafe/signboard.png", AssetLoadPriority.Low);
        }
    }
    
    internal static void OnAssetReady(object sender, AssetReadyEventArgs e)
    {
        if (e.Name.IsDirectlyUnderPath(ModKeys.ASSETS_NPCSCHEDULE_PREFIX))
        {
            string npcname = e.Name.Name.Split('/').Last();
        }
    }


    internal void OnTimeChanged(object sender, TimeChangedEventArgs e)
    {
        if (!Context.IsMainPlayer) return;

        int minutesTillCloses = SUtility.CalculateMinutesBetweenTimes(Game1.timeOfDay, Cafe.ClosingTime.Value);
        int minutesTillOpens = SUtility.CalculateMinutesBetweenTimes(Game1.timeOfDay, Cafe.OpeningTime.Value);
        int minutesSinceLastVisitors = SUtility.CalculateMinutesBetweenTimes(Cafe.LastTimeCustomersArrived, Game1.timeOfDay);
        float percentageOfTablesFree = (float)Mod.Cafe.Tables.Count(t => !t.IsReserved) / Cafe.Tables.Count;

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
    }

    internal void OnFurnitureListChanged(object sender, FurnitureListChangedEventArgs e)
    {
        if (!Context.IsMainPlayer) return;

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

                    FurnitureTable table = Utility.IsTableTracked(facingFurniture, e.Location, out FurnitureTable existing)
                        ? existing
                        : new FurnitureTable(facingFurniture, e.Location.Name);

                    table.AddChair(f);
                    Cafe.TryAddTable(table);
                }
                else if (Utility.IsTable(f))
                {
                    if (!Utility.IsTableTracked(f, e.Location, out _))
                    {
                        FurnitureTable table = new FurnitureTable(f, e.Location.Name);
                        if (table.Seats.Count > 0)
                            Cafe.TryAddTable(table);
                    }

                }
            }
        }
    }

    internal static void OnModMessageReceived(object sender, ModMessageReceivedEventArgs e)
    {
        if (e.FromModID != ModManifest.UniqueID)
            return;

        if (e.Type == "VisitorDoEmote" && !Context.IsMainPlayer)
        {
            try
            {
                var (key, emote) = e.ReadAs<KeyValuePair<string, int>>();
                Game1.getCharacterFromName(key)?.doEmote(emote);
            }
            catch
            {
                Log.Debug("Invalid message from host", LogLevel.Error);
            }
        }
    }

    internal void LoadContentPacks()
    {
        CustomersData = new Dictionary<string, BusCustomerData>();

        foreach (IContentPack contentPack in ModHelper.ContentPacks.GetOwned())
        {
            Log.Debug($"Loading content pack {contentPack.Manifest.Name} by {contentPack.Manifest.Author}");
            var modelsInPack = new DirectoryInfo(Path.Combine(contentPack.DirectoryPath, "Customers")).GetDirectories();
            foreach (var modelFolder in modelsInPack)
            {
                CustomerModel model = contentPack.ReadJsonFile<CustomerModel>(Path.Combine("Customers", modelFolder.Name, "customer.json"));
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
                    model.PortraitName = ModHelper.ModContent.GetInternalAssetName(Path.Combine("assets", "Portraits", portraitName + ".png")).Name;
                }

                CustomersData[model.Name] = new BusCustomerData()
                {
                    Model = model
                };
            }
        }
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
        Mod.SpaceCore.RegisterCustomProperty(
            typeof(Farm),
            "Cafe",
            typeof(NetRef<Cafe>),
            AccessTools.Method(typeof(CafeState), nameof(get_Cafe)),
            AccessTools.Method(typeof(CafeState), nameof(set_Cafe)));
    }

    public static NetRef<Cafe> get_Cafe(this Farm farm)
    {
        var holder = Values.GetOrCreateValue(farm);
        return holder!.Value;
    }

    public static void set_Cafe(this Farm farm, NetRef<Cafe> value)
    {
        Log.Warn("Setting Cafe field for Farm. Should this be happening?");
        var holder = Values.GetOrCreateValue(farm);
        holder!.Value = value;
    }
}
