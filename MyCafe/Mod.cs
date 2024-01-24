global using SUtility = StardewValley.Utility;
global using SObject = StardewValley.Object;

using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MyCafe.Locations;
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
using StardewValley.Locations;



namespace MyCafe;

public class Mod : StardewModdingAPI.Mod
{
    internal static ISpaceCoreApi SpaceCore;

    internal new static IMonitor Monitor;
    internal static IModHelper ModHelper;
    internal new static IManifest ModManifest;

    internal static Cafe Cafe
        => Game1.getFarm().get_Cafe().Value;

    internal static Texture2D Sprites;

    internal static Dictionary<string, BusCustomerData> CustomersData;

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

        helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
        helper.Events.GameLoop.DayStarted += OnDayStarted;
        helper.Events.GameLoop.Saving += OnSaving;
        helper.Events.GameLoop.ReturnedToTitle += OnReturnedToTitle;
        helper.Events.Multiplayer.ModMessageReceived += OnModMessageReceived;
        helper.Events.Content.AssetRequested += OnAssetRequested;
        helper.Events.Content.AssetReady += OnAssetReady;
        helper.Events.Display.RenderedWorld += OnRenderedWorld;
        helper.Events.Input.ButtonPressed += Debug.ButtonPress;

        Sprites = helper.ModContent.Load<Texture2D>("assets/sprites.png");
    }


    private void OnGameLaunched(object sender, GameLaunchedEventArgs e)
    {
        SpaceCore = ModHelper.ModRegistry.GetApi<ISpaceCoreApi>("spacechase0.SpaceCore")!;
        SpaceCore.RegisterSerializerType(typeof(Cafe));
        SpaceCore.RegisterSerializerType(typeof(CafeLocation));
        CafeState.Register();

        ModConfig.InitializeGmcm();

        GameLocation.RegisterTileAction(ModKeys.SIGNBOARD_BUILDING_CLICK_EVENT_KEY, delegate
        {
            Game1.activeClickableMenu = new CafeMenu();
            return true;
        });
    }

    private void OnSaveLoaded(object sender, SaveLoadedEventArgs e)
    {
        if (Context.IsMainPlayer)
        {
            LoadContentPackBusCustomers();
            ModHelper.Events.World.FurnitureListChanged += OnFurnitureListChanged;
            ModHelper.Events.GameLoop.TimeChanged += OnTimeChanged;
            ModHelper.Events.GameLoop.DayEnding += OnDayEnding;

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
    }

    internal void OnDayStarted(object sender, DayStartedEventArgs e)
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

            if (Cafe.Enabled)
                Cafe.DayUpdate();
        }
        
    }

    internal void OnSaving(object sender, SavingEventArgs e)
    {
        if (Context.IsMainPlayer)
        {
            Game1.player.modData[ModKeys.MODDATA_OPENCLOSETIMES] = $"{Cafe.OpeningTime.Value}|{Cafe.ClosingTime.Value}";
            //Game1.player.modData[ModKeys.MODDATA_MENUITEMSLIST] = string.Join('|', Cafe.MenuItems.Where(x => x != null).Select(x => x.ItemId));
        }
    }

    internal void OnDayEnding(object sender, DayEndingEventArgs e)
    {
        StardewValley.Utility.ForEachLocation(delegate(GameLocation loc)
        {
            for (int i = loc.characters.Count - 1; i >= 0; i--)
            {
                if (loc.characters[i] is Customer)
                {
                    loc.characters.RemoveAt(i);
                }
            }
            return true;
        });
    }

    internal void OnReturnedToTitle(object sender, ReturnedToTitleEventArgs e)
    {
        ModHelper.Events.World.FurnitureListChanged -= OnFurnitureListChanged;
        ModHelper.Events.GameLoop.TimeChanged -= OnTimeChanged;
        ModHelper.Events.GameLoop.DayEnding -= OnDayEnding;
    }

    internal static void OnAssetRequested(object sender, AssetRequestedEventArgs e)
    {
        // Schedules
        if (e.Name.IsDirectlyUnderPath(ModKeys.ASSETS_NPCSCHEDULE_PREFIX))
        {
            string npcname = e.Name.Name.Split('/').Last();
            var file = ModHelper.Data.ReadJsonFile<VillagerCustomerData>("assets/Schedules/" + npcname + ".json");
            if (file != null)
            {
                e.LoadFrom(() => file, AssetLoadPriority.Low);
            }
        }

        // Buildings
        else if (e.Name.IsEquivalentTo("Data/Buildings"))
        {
            e.Edit(asset =>
            {
                var data = asset.AsDictionary<string, BuildingData>();
                data.Data[ModKeys.CAFE_BUILDING_BUILDING_ID] = ModHelper.ModContent.Load<BuildingData>("assets/Cafe/cafebuilding.json");
                data.Data[ModKeys.CAFE_SIGNBOARD_BUILDING_ID] = ModHelper.ModContent.Load<BuildingData>("assets/Cafe/signboard.json");
            }, AssetEditPriority.Early);
        }

        // Cafe
        else if (e.Name.IsEquivalentTo(ModKeys.CAFE_BUILDING_TEXTURE_NAME))
        {
            e.LoadFromModFile<Texture2D>("assets/Cafe/cafebuilding.png", AssetLoadPriority.Medium);
        }
        else if (e.Name.IsEquivalentTo($"Maps/{ModKeys.CAFE_MAP_NAME}"))
        {
            e.LoadFromModFile<Map>("assets/Cafe/cafemap.tmx", AssetLoadPriority.Medium);
        }

        // Signboard
        else if (e.Name.IsEquivalentTo(ModKeys.CAFE_SIGNBOARD_TEXTURE_NAME))
        {
            e.LoadFromModFile<Texture2D>("assets/Cafe/signboard.png", AssetLoadPriority.Medium);
        }
    }
    
    internal static void OnAssetReady(object sender, AssetReadyEventArgs e)
    {
        if (e.Name.IsDirectlyUnderPath(ModKeys.ASSETS_NPCSCHEDULE_PREFIX))
        {
            string npcname = e.Name.Name.Split('/').Last();
            //Mod.Customers.VillagerCustomers.VillagerData[npcname] = Game1.content.Load<VillagerCustomerData>(ModKeys.ASSETS_NPCSCHEDULE_PREFIX + npcname);
        }
    }


    internal void OnTimeChanged(object sender, TimeChangedEventArgs e)
    {
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

    internal static void OnModMessageReceived(object sender, ModMessageReceivedEventArgs e)
    {
        if (e.FromModID != ModManifest.UniqueID)
            return;

        if (e.Type == "VisitorDoEmote" && !Context.IsMainPlayer)
        {
            try
            {
                var (key, emote) = e.ReadAs<KeyValuePair<string, int>>();
                NPC npc = Game1.getCharacterFromName(key);

                npc?.doEmote(emote);
            }
            catch
            {
                Log.Debug("Invalid message from host", LogLevel.Error);
            }
        }
    }

    internal static bool OpenCafeMenuTileAction(GameLocation location, string[] args, Farmer player, Point tile)
    {
        if (!Context.IsMainPlayer)
            return false;

        if (Game1.activeClickableMenu == null && Context.IsPlayerFree)
        {
            Log.Debug("Opened cafe menu menu!");
            // Game1.activeClickableMenu = new CafeMenu();
        }

        return true;
    }

    
    internal static bool LoadContentPackBusCustomers()
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

        return true;
    }
}

public static class CafeState
{
    internal class Holder
    {
        public NetRef<Cafe> Value = new(new Cafe());
    }

    internal static ConditionalWeakTable<Farm, Holder> values = new();

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
        var holder = values.GetOrCreateValue(farm);
        return holder!.Value;
    }

    public static void set_Cafe(this Farm farm, NetRef<Cafe> value)
    {
        var holder = values.GetOrCreateValue(farm);
        holder!.Value = value;
    }
}
