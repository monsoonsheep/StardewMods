using HarmonyLib;
using Microsoft.Xna.Framework.Graphics;
using StardewCafe.Framework.Objects;
using StardewCafe.Framework;
using StardewCafe.Patching;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley.Buffs;
using StardewValley;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Xna.Framework;
using StardewCafe.Framework.Customers;
using VisitorFramework.Framework.UI;

namespace StardewCafe
{
    public class ModEntry : Mod
    {
        internal new static IMonitor Monitor;
        internal static IModHelper ModHelper;
        internal new static IManifest ModManifest;

        internal static Config Config;

        internal static Texture2D Sprites;

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
            GameLocation.RegisterTileAction(ModKeys.SIGNBOARD_BUILDING_CLICK_EVENT_KEY, OpenCafeMenu);
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
                name: I18n.Menu_VisitorFrequency,
                tooltip: I18n.Menu_VisitorFrequencyTooltip,
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

        private static void OnGameLaunched(object sender, GameLaunchedEventArgs e)
        {
            if (Context.IsMainPlayer)
            {
                InitConfig();
            }
        }

        private static void OnSaveLoaded(object sender, SaveLoadedEventArgs e)
        {
            if (!Context.IsMainPlayer)
            {
                //CafeManager.CurrentVisitors = CafeManager.GetAllVisitorsInGame();
                return;
            }

            CafeManager.Tables = new List<Table>();
            CafeManager.CafeLocations = new List<GameLocation>();
            
            LoadValuesFromModData();
        }

        private static void OnDayStarted(object sender, DayStartedEventArgs e)
        {
            CafeManager.UpdateCafeLocation();
            Logger.Log($"Cafe locations are {string.Join(", ", CafeManager.CafeLocations.Select(l => l.Name))}");

            if (!Context.IsMainPlayer)
                return;

            CafeManager.DayUpdate();
            // TODO: look at NPC schedules. 

        }

        private static void OnDayEnding(object sender, DayEndingEventArgs e)
        {
            CafeManager.RemoveAllVisitors();
        }

        private static void OnSaving(object sender, EventArgs e)
        {
            // Menu Items TODO: Make sure they are in the same order as saved (Where() is ordered)
            Game1.player.modData[ModKeys.MODDATA_MENUITEMSLIST] = string.Join(" ", CafeManager.GetMenuItems().Select(i => i.ItemId));

            // Opening and Closing Times
            Game1.player.modData[ModKeys.MODDATA_OPENCLOSETIMES] = $"{CafeManager.OpeningTime} {CafeManager.ClosingTime}";

            // NPCs' last visited date in the format "Name year,season,dayOfMonth/Name year,season,dayOfMonth"
            Game1.player.modData[ModKeys.MODDATA_NPCSLASTVISITEDDATES] = string.Join(' ',
                CafeManager.NpcVisitorSchedules.Select(pair =>
                    $"{pair.Key} {pair.Value.LastVisitedDate.Year},{pair.Value.LastVisitedDate.SeasonIndex},{pair.Value.LastVisitedDate.DayOfMonth}"));
        }

        private static void OnUpdateTicked(object sender, UpdateTickedEventArgs e)
        {
            //if (!Context.IsMainPlayer && ClientShouldUpdateVisitors)
            //{
            //    CurrentVisitors = CafeManager.GetAllVisitorsInGame();
            //    ClientShouldUpdateVisitors = false;
            //}
        }

        private static void OnTimeChanged(object sender, TimeChangedEventArgs e)
        {
            
        }

        private static void OnLocationListChanged(object sender, LocationListChangedEventArgs e)
        {
            
        }

        private static void OnFurnitureListChanged(object sender, FurnitureListChangedEventArgs e)
        {
            if (Context.IsMainPlayer && CafeManager.CafeLocations.Any(l => l.Equals(e.Location)))
                CafeManager.HandleFurnitureChanged(e.Added, e.Removed, e.Location);
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

        private static void OnModMessageReceived(object sender, ModMessageReceivedEventArgs e)
        {
            if (e.FromModID != ModManifest.UniqueID) return;

            if (e.Type.StartsWith("UpdateVisitorInfo") && !Context.IsMainPlayer)
            {
                var split = e.Type.Split('/');
                Customer c = CafeManager.GetVisitorFromName(split[1]);
                if (c == null)
                {
                    Logger.Log("Couldn't get Visitor to update");
                    return;
                }
               
                switch (split[2])
                {
                    case nameof(c.OrderItem):
                        c.OrderItem = GetItem(e.ReadAs<string>());
                        break;
                }
            }
            else if (e.Type == "ClickTable" && Context.IsMainPlayer)
            {
                try
                {
                    var data = e.ReadAs<Dictionary<string, string>>();
                    Farmer who = Game1.getFarmer(long.Parse(data["farmer"]));
                    MatchCollection matches = Regex.Matches(data["table"], @"\d+");

                    if (matches.Count == 2)
                    {
                        // Also add functionality for map tables
                        Table table = CafeManager.GetTableAt(who.currentLocation, new Vector2(float.Parse(matches[0].Value), float.Parse(matches[1].Value)));
                        CafeManager.FarmerClickTable(table, who);
                    }
                }
                catch
                {
                    Logger.Log("Invalid message from host", LogLevel.Warn);
                }
            }
            else if (e.Type == "VisitorDoEmote" && !Context.IsMainPlayer)
            {
                try
                {
                    var info = e.ReadAs<Dictionary<string, string>>();
                    Visitor c = CafeManager.GetVisitorFromName(info["name"]);
                    c?.doEmote(int.Parse(info["emote"]));
                }
                catch
                {
                    Logger.Log("Invalid message from host", LogLevel.Warn);
                }
            }
        }

        private static void OnPeerConnected(object sender, PeerConnectedEventArgs e)
        {
            return;
        }

        private static void LoadValuesFromModData()
        {
            if (Game1.player.modData.TryGetValue(ModKeys.MODDATA_MENUITEMSLIST, out string menuItemsString))
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

            if (Game1.player.modData.TryGetValue(ModKeys.MODDATA_OPENCLOSETIMES, out string openCloseTimes))
            {
                CafeManager.OpeningTime = int.Parse(openCloseTimes.Split(' ')[0]);
                CafeManager.ClosingTime = int.Parse(openCloseTimes.Split(' ')[1]);
            }

            if (Game1.player.modData.TryGetValue(ModKeys.MODDATA_NPCSLASTVISITEDDATES, out string npcsLastVisited))
            {
                var split = npcsLastVisited.Split(' ');
                for (var index = 0; index < split.Length; index += 2)
                {
                    string npcName = split[index];
                    string[] date = split[index + 1].Split(',');

                    CafeManager.NpcVisitorSchedules[npcName].LastVisitedDate =
                        new WorldDate(int.Parse(date[0]), (Season)int.Parse(date[1]), int.Parse(date[2]));
                }
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
