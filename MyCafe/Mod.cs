using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonsoonSheep.Stardew.Common;
using MonsoonSheep.Stardew.Common.Patching;
using MyCafe.Customers;
using MyCafe.Customers.Data;
using MyCafe.Enums;
using MyCafe.Interfaces;
using MyCafe.Locations.Objects;
using MyCafe.Patching;
using MyCafe.UI;
using Netcode;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.GameData.Buildings;
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

    internal ConfigModel Config = null!;
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
        this.Config = helper.ReadConfig<ConfigModel>();
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
        events.GameLoop.Saving += this.OnSaving;
        events.Multiplayer.ModMessageReceived += this.OnModMessageReceived;
        events.Content.AssetRequested += this.OnAssetRequested;
        events.Content.AssetReady += OnAssetReady;
        events.Display.RenderedWorld += this.OnRenderedWorld;
        events.Input.ButtonPressed += Debug.ButtonPress;

        events.World.FurnitureListChanged += this.OnFurnitureListChanged;
        events.GameLoop.TimeChanged += this.OnTimeChanged;
        events.GameLoop.DayEnding += this.OnDayEnding;

        Sprites = helper.ModContent.Load<Texture2D>(Path.Combine("assets", "sprites.png"));
    }

    private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
    {
        this.SpaceCore = this.Helper.ModRegistry.GetApi<ISpaceCoreApi>("spacechase0.SpaceCore")!;
        this.SpaceCore.RegisterSerializerType(typeof(Cafe));
        this.SpaceCore.RegisterSerializerType(typeof(CafeLocation));
        CafeState.Register();

        ModConfig.InitializeGmcm(this.Helper, this.ModManifest);
        GameLocation.RegisterTileAction(ModKeys.SIGNBOARD_BUILDING_CLICK_EVENT_KEY, CafeMenu.Action_OpenCafeMenu);
    }

    private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
    {
        if (!Context.IsMainPlayer)
            return;

        this.LoadContentPacks();

        if (Game1.player.modData.TryGetValue(ModKeys.MODDATA_OPENCLOSETIMES, out string? openclose))
        {
            string[] split = ArgUtility.SplitBySpace(openclose);
            if (int.TryParse(ArgUtility.Get(split, 0, defaultValue: "830", allowBlank: false), out int open)
                && int.TryParse(ArgUtility.Get(split, 1, defaultValue: "2200", allowBlank: false), out int close))
            {
                Cafe.OpeningTime.Set(open);
                Cafe.ClosingTime.Set(close);
            }
        }

        if (Game1.player.modData.TryGetValue(ModKeys.MODDATA_MENUITEMSLIST, out string? menuitems))
        {
            Cafe.MenuItems.Clear();
        }

        Cafe.Initialize(this.Helper, this.CustomersData);
    }

    internal void OnDayStarted(object? sender, DayStartedEventArgs e)
    {
        if (!Context.IsMainPlayer)
            return;

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

    internal void OnSaving(object? sender, SavingEventArgs e)
    {
        if (!Context.IsMainPlayer) return;

        Game1.player.modData[ModKeys.MODDATA_OPENCLOSETIMES] = $"{Cafe.OpeningTime.Value} {Cafe.ClosingTime.Value}";
        Game1.player.modData[ModKeys.MODDATA_MENUITEMSLIST] = "";
    }

    internal void OnDayEnding(object? sender, DayEndingEventArgs e)
    {
        if (!Context.IsMainPlayer) return;

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
            var file = this.Helper.Data.ReadJsonFile<VillagerCustomerData>(Path.Combine("assets", "Schedules", npcname + ".json"));
            if (file != null)
                e.LoadFrom(() => file, AssetLoadPriority.Low);
        }

        // Buildings data (Cafe and signboard)
        else if (e.Name.IsEquivalentTo("Data/Buildings"))
        {
            e.Edit(asset =>
            {
                var data = asset.AsDictionary<string, BuildingData>();
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
            e.LoadFromModFile<Texture2D>("assets/Cafe/signboard.png", AssetLoadPriority.Low);
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

        if (IsPlacingSignBoard)
        {
            foreach (Vector2 tile in TileHelper.GetCircularTileGrid(new Vector2((Game1.viewport.X + Game1.getOldMouseX(false)) / 64, (Game1.viewport.Y + Game1.getOldMouseY(false)) / 64), this.Config.DistanceForSignboardToRegisterTables))
            {
                // get tile area in screen pixels
                Rectangle area = new((int)(tile.X * Game1.tileSize - Game1.viewport.X), (int)(tile.Y * Game1.tileSize - Game1.viewport.Y), Game1.tileSize, Game1.tileSize);

                // choose tile color
                Color color = Color.LightCyan;

                // draw background
                e.SpriteBatch.DrawLine(area.X, area.Y, new Vector2(area.Width, area.Height), color * 0.2f);

                // draw border
                int borderSize = 1;
                Color borderColor = color * 0.5f;
                e.SpriteBatch.DrawLine(area.X, area.Y, new Vector2(area.Width, borderSize), borderColor); // top
                e.SpriteBatch.DrawLine(area.X, area.Y, new Vector2(borderSize, area.Height), borderColor); // left
                e.SpriteBatch.DrawLine(area.X + area.Width, area.Y, new Vector2(borderSize, area.Height), borderColor); // right
                e.SpriteBatch.DrawLine(area.X, area.Y + area.Height, new Vector2(area.Width, borderSize), borderColor); // bottom
            }
        }
    }

    internal void OnFurnitureListChanged(object? sender, FurnitureListChangedEventArgs e)
    {
        if (!Context.IsMainPlayer) return;

        if (e.Location.Equals(Cafe.Indoor) || e.Location.Equals(Cafe.Outdoor))
        {
            foreach (var f in e.Removed)
            {
                if (Utility.IsChair(f))
                {
                    FurnitureSeat? trackedChair = Cafe.Tables
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
                    Vector2 tablePos = f.TileLocation + CommonHelper.DirectionIntToDirectionVector(f.currentRotation.Value) * new Vector2(1, -1);

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

                    FurnitureTable table =
                        Utility.IsTableTracked(facingFurniture, e.Location, out FurnitureTable existing)
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

    internal void OnModMessageReceived(object? sender, ModMessageReceivedEventArgs e)
    {
        if (e.FromModID != this.ModManifest.UniqueID)
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
        var holder = Values.GetOrCreateValue(farm);
        return holder!.Value;
    }

    public static void set_Cafe(this Farm farm, NetRef<Cafe> value)
    {
        Log.Error("Setting Cafe field for Farm. Should this be happening?");
        var holder = Values.GetOrCreateValue(farm);
        holder!.Value = value;
    }
}
