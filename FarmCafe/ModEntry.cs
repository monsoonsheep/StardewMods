using FarmCafe.Framework.Managers;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using static FarmCafe.Framework.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using FarmCafe.Framework;
using FarmCafe.Framework.Characters;
using FarmCafe.Framework.Objects;
using FarmCafe.Framework.UI;
using Rectangle = Microsoft.Xna.Framework.Rectangle;
using Object = StardewValley.Object;
using FarmCafe.Patching;

namespace FarmCafe
{
    /// <summary>The mod entry point.</summary>
    internal sealed class ModEntry : Mod
    {
        internal new static IMonitor Monitor;
        internal static IModHelper ModHelper;
        internal new static IManifest ModManifest;

        internal static Config Config;

        internal static Texture2D Sprites;

        internal static CafeManager CafeManager;

        internal static NPC HelperNpc;

        // Name to list of <startTime, endTime>
        internal static Dictionary<string, List<KeyValuePair<int, int>>> CustomerableNpcsToday;

        /// <inheritdoc/>
        public override void Entry(IModHelper helper)
        {
            Monitor = base.Monitor;
            ModHelper = helper;
            ModManifest = base.ModManifest;
            Logger.Monitor = Monitor;

            Config = helper.ReadConfig<Config>();

            I18n.Init(helper.Translation);
            // Harmony patches
            try
            {
                var harmony = new Harmony(ModManifest.UniqueID);
                new GameLocationPatches().ApplyAll(harmony);
                new CharacterPatches().ApplyAll(harmony);
                new UtilityPatches().ApplyAll(harmony);
                new FurniturePatches().ApplyAll(harmony);
            }
            catch (Exception e)
            {
                Logger.Log($"Couldn't patch methods - {e}", LogLevel.Error);
                return;
            }

            helper.Events.GameLoop.GameLaunched += OnGameLaunched;
            helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
            helper.Events.GameLoop.DayStarted += OnDayStarted;
            helper.Events.GameLoop.DayEnding += OnDayEnding;
            helper.Events.GameLoop.Saving += OnSaving;


            helper.Events.Display.RenderedWorld += OnRenderedWorld;
            helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
            helper.Events.GameLoop.TimeChanged += OnTimeChanged;
            helper.Events.Input.ButtonPressed += Debug.ButtonPress;
            helper.Events.Content.AssetRequested += AssetManager.OnAssetRequested;
            helper.Events.Content.AssetReady += AssetManager.OnAssetReady;


            helper.Events.World.LocationListChanged += OnLocationListChanged;
            helper.Events.World.FurnitureListChanged += OnFurnitureListChanged;

            // Sync
            helper.Events.Multiplayer.PeerConnected += OnPeerConnected;
            helper.Events.Multiplayer.ModMessageReceived += OnModMessageReceived;

            Sprites = helper.ModContent.Load<Texture2D>("assets/sprites.png");
            GameLocation.RegisterTileAction("FarmCafe_OpenCafeMenu", OpenCafeMenu);
        }

        private static void OnGameLaunched(object sender, GameLaunchedEventArgs e)
        {
            if (Context.IsMainPlayer)
            {
                InitConfig();
            }
        }

        private static void InitConfig()
        {
            string GetFrequencyText(int n)
            {
                return n switch
                {
                    1 => "Very Low",
                    2 => "Low",
                    3 => "Medium",
                    4 => "High",
                    5 => "Very High",
                    _ => "???"
                };
            }

            // get Generic Mod Config Menu's API (if it's installed)
            var configMenu = ModHelper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
            if (configMenu is null)
                return;

            // register mod
            configMenu.Register(
                mod: ModManifest,
                reset: () => Config = new Config(),
                save: () => ModHelper.WriteConfig(Config)
            );

            // add some config options
            configMenu.AddNumberOption(
                mod: ModManifest,
                name: I18n.Menu_CustomerFrequency,
                tooltip: I18n.Menu_CustomerFrequencyTooltip,
                getValue: () => Config.CustomerSpawnFrequency,
                setValue: value => Config.CustomerSpawnFrequency = value,
                min: 1, max: 5, 
                formatValue: GetFrequencyText
            );
            configMenu.AddNumberOption(
                mod: ModManifest,
                name: I18n.Menu_NpcFrequency,
                tooltip: I18n.Menu_NpcFrequency_Tooltip,
                getValue: () => Config.NpcCustomerSpawnFrequency,
                setValue: value => Config.NpcCustomerSpawnFrequency = value,
                min: 1, max: 5, 
                formatValue: GetFrequencyText
            );
        }

        private static void OnSaveLoaded(object sender, SaveLoadedEventArgs e)
        {
            if (!Context.IsMainPlayer)
            {
                CafeManager.CurrentCustomers = CafeManager.GetAllCustomersInGame();
                return;
            }

            CafeManager.Tables = new();
            CafeManager.CafeLocations = new();
            CafeManager.CurrentCustomers = new();

            CafeManager = new CafeManager();
            LoadValuesFromModData();
            
            AssetManager.LoadCustomerModels(ModHelper, ref CafeManager.CustomerModels);
            AssetManager.LoadContentPacks(ModHelper, ref CafeManager.CustomerModels);
            AssetManager.LoadNpcSchedules(ModHelper);
        }

        private static void OnDayStarted(object sender, DayStartedEventArgs e)
        {
            CafeManager.UpdateCafeLocation();
            Logger.Log($"Cafe locations are {string.Join(", ", CafeManager.CafeLocations.Select(l => l.Name))}");

            if (!Context.IsMainPlayer)
                return;

            CafeManager.DayUpdate();
            // look at NPC schedules. 
        }

        private static void OnDayEnding(object sender, DayEndingEventArgs e)
        {
            if (Context.IsMainPlayer)
            {
                CafeManager.RemoveAllCustomers();
            }
        }

        private static void OnSaving(object sender, EventArgs e)
        {
            if (!Context.IsMainPlayer) 
                return;

            // Menu Items TODO: Make sure they are in the same order as saved (Where() is ordered)
            Game1.player.modData["FarmCafeMenuItems"] = string.Join(" ", CafeManager.GetMenuItems().Select(i => i.ItemId));
            // Opening and Closing Times
            Game1.player.modData["FarmCafeOpenCloseTimes"] = $"{CafeManager.OpeningTime} {CafeManager.ClosingTime}";
        }

        private static void OnModMessageReceived(object sender, ModMessageReceivedEventArgs e)
        {
            if (e.FromModID != ModManifest.UniqueID) return;

            if (e.Type == "UpdateCustomers")
            {
                //ClientShouldUpdateCustomers = true;
            }
            else if (e.Type == "RemoveCustomers")
            {
                CafeManager.CurrentCustomers.Clear();
            }
            else if (e.Type.StartsWith("UpdateCustomerInfo") && !Context.IsMainPlayer)
            {
                Customer c = CafeManager.GetAllCustomersInGame().FirstOrDefault(c => c.Name == e.Type.Split('/')[1]);
                if (c == null)
                {
                    Logger.Log("Couldn't get customer to update");
                    return;
                }
               
                switch (e.Type.Split('/')[2])
                {
                    case nameof(c.OrderItem):
                        c.OrderItem = GetItem(e.ReadAs<string>());
                        break;
                }
            }
            else if (e.Type == "ClickTable" && Context.IsMainPlayer)
            {
                var info = e.ReadAs<Dictionary<string, string>>();
                long.TryParse(info["farmer"], out long farmerId);
                Farmer who = Game1.getFarmer(farmerId);

                MatchCollection matches = Regex.Matches(info["table"], @"\d+");
                if (matches.Count == 2 &&
                    float.TryParse(matches[0].Value, out float x) &&
                    float.TryParse(matches[1].Value, out float y))
                {
                    // Also add functionality for map tables
                    Table table = CafeManager.GetTableAt(who.currentLocation, new Vector2(x, y));
                    if (table != null)
                        CafeManager.FarmerClickTable(table, who);
                }
            }
            else if (e.Type == "CustomerDoEmote" && !Context.IsMainPlayer)
            {
                var info = e.ReadAs<Dictionary<string, string>>();
                Customer c = CafeManager.GetAllCustomersInGame().FirstOrDefault(c => c.Name == info["name"]);
                c?.doEmote(int.Parse(info["emote"]));
            }
        }

        private static void OnRenderedWorld(object sender, RenderedWorldEventArgs e)
        {
            // get list of reserved tables with center coords
            foreach (var table in CafeManager.Tables)
            {
                // TODO Sync IsReadyToOrder for clients
                if (table.IsReadyToOrder && Game1.currentLocation.Equals(table.CurrentLocation))
                {
                    Vector2 offset = new Vector2(0,
                                (float)Math.Round(4f * Math.Sin(Game1.currentGameTime.TotalGameTime.TotalMilliseconds / 250.0)));

                    e.SpriteBatch.Draw(
                        Game1.mouseCursors,
                        Game1.GlobalToLocal(table.GetCenter()  + new Vector2(-8, -64)) + offset,
                        new Rectangle(402, 495, 7, 16),
                        Color.Crimson,
                        0f,
                        new Vector2(1f, 4f),
                        4f,
                        SpriteEffects.None,
                        1f);
                }
            }
        }

        private static void OnUpdateTicked(object sender, UpdateTickedEventArgs e)
        {
            //if (!Context.IsMainPlayer && ClientShouldUpdateCustomers)
            //{
            //    CurrentCustomers = CafeManager.GetAllCustomersInGame();
            //    ClientShouldUpdateCustomers = false;
            //}
        }

        private static void OnTimeChanged(object sender, TimeChangedEventArgs e)
        {
            if (!Context.IsMainPlayer)
                return;

            // spawn customers depending on probability logic
            CafeManager.TrySpawnCustomers();
        }

        private static void OnPeerConnected(object sender, PeerConnectedEventArgs e)
        {
            return;
        }

        private static void OnLocationListChanged(object sender, LocationListChangedEventArgs e)
        {
            foreach (var removed in e.Removed)
            {
                if (IsLocationCafe(removed))
                {
                    CafeManager.UpdateCafeLocation();
                }
            }
        }

        private static void OnFurnitureListChanged(object sender, FurnitureListChangedEventArgs e)
        {
            if (Context.IsMainPlayer && CafeManager.CafeLocations.Any(l => l.Equals(e.Location)))
                CafeManager.HandleFurnitureChanged(e.Added, e.Removed, e.Location);
        }
        
        private static void LoadValuesFromModData()
        {
            if (Game1.player.modData.TryGetValue("FarmCafeMenuItems", out string menuItemsString))
            {
                var itemIds = menuItemsString.Split(' ');
                if (itemIds.Length == 0)
                {
                    Logger.Log("The menu for the cafe has nothing in it!");
                }
                else
                {
                    foreach (var id in itemIds)
                    {
                        try
                        {
                            Item item = new Object(id, 1);
                            CafeManager.AddToMenu(item);
                        }
                        catch
                        {
                            Logger.Log("Invalid item ID in player's modData.", LogLevel.Warn);
                            break;
                            
                        }
                    }
                }
            }

            if (Game1.player.modData.TryGetValue("FarmCafeOpenCloseTimes", out string openCloseTimes))
            {
                CafeManager.OpeningTime = int.Parse(openCloseTimes.Split(' ')[0]);
                CafeManager.ClosingTime = int.Parse(openCloseTimes.Split(' ')[1]);
            }
        }

        internal static bool OpenCafeMenu(GameLocation location, string[] args, Farmer player, Point tile)
        {
            if (!Context.IsMainPlayer)
                return false;

            if (Game1.activeClickableMenu == null && Context.IsPlayerFree)
            {
                Logger.Log("Open menu!");
                Game1.activeClickableMenu = new CafeMenu();
            }

            return true;
        }
    }
}