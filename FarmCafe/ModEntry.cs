using FarmCafe.Framework.Interfaces;
using FarmCafe.Framework.Managers;
using FarmCafe.Framework.Models;
using FarmCafe.Framework.Patching;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Objects;
using StardewValley.Buildings;
using static FarmCafe.Framework.Utilities.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using FarmCafe.Framework.Characters;
using FarmCafe.Framework.Multiplayer;
using FarmCafe.Framework.Objects;
using FarmCafe.Framework.UI;
using Sickhead.Engine.Util;
using StardewValley.Menus;
using xTile.Dimensions;
using SolidFoundations.Framework.Interfaces.Internal;
using Rectangle = Microsoft.Xna.Framework.Rectangle;
using Object = StardewValley.Object;
using StardewValley.Monsters;
using FarmCafe.Locations;
using SolidFoundations.Framework.Models.ContentPack;

namespace FarmCafe
{
    /// <summary>The mod entry point.</summary>
    internal sealed class ModEntry : Mod
    {
        internal new static IMonitor Monitor;
        internal static IModHelper ModHelper;
        internal static IApi SfApi;
        internal new static IManifest ModManifest;

        internal static Texture2D Sprites;

        internal static CafeManager CafeManager;

        // To be synced in multiplayer
        internal static List<GameLocation> CafeLocations = new();
        internal static List<Customer> CurrentCustomers = new List<Customer>();
        internal static List<Table> Tables = new();
        internal static IList<Item> MenuItems = new List<Item>(new Item[27]);
        internal static IList<Item> RecentlyAddedMenuItems = new List<Item>(new Item[9]);

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

            Sprites = helper.ModContent.Load<Texture2D>("assets/cursors.png");

        }

        private static void OnGameLaunched(object sender, GameLaunchedEventArgs e)
        {
            if (Context.IsMainPlayer)
            {
                PrepareSolidFoundationsApi();
            }
        }

        private static void OnSaveLoaded(object sender, SaveLoadedEventArgs e)
        {
            if (!Context.IsMainPlayer)
            {
                CurrentCustomers = Sync.GetAllCustomersInGame();
                return;
            }

            Tables = new();
            CafeLocations = new();
            CurrentCustomers = new();

            MenuItems = new List<Item>(new Item[27]);
            RecentlyAddedMenuItems = new List<Item>(new Item[9]);

            
            CafeManager = new CafeManager();

            AssetManager.LoadContentPacks(ModHelper, ref CafeManager.CustomerModels);
            AssetManager.LoadNpcSchedules(ModHelper);
        }

        private static void OnDayStarted(object sender, DayStartedEventArgs e)
        {
            CafeManager.UpdateCafeLocation();
            Logger.Log($"Cafe locations are {string.Join(", ", CafeLocations.Select(l => l.Name))}");

            if (!Context.IsMainPlayer)
                return;

            CafeManager.DayUpdate();
            // look at NPC schedules. 
        }

        /// <summary>
        /// Remove all customers before Solid Foundations can try to serialize them (it serializes locations, and the NPCs in them)
        /// </summary>
        /// <remarks>The <see cref="EventPriority"/> is set to High + 1, because SF has set its to High. This always has to go first</remarks>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        [EventPriority(EventPriority.High + 1)]
        private static void OnDayEnding(object sender, DayEndingEventArgs e)
        {
            if (Context.IsMainPlayer)
            {
                CafeManager?.RemoveAllCustomers();
            }
        }

        private static void OnSaving(object sender, EventArgs e)
        {
            if (!Context.IsMainPlayer) 
                return;

            // TODO: Make sure they are in the same order as saved (Where() is ordered)
            string itemsString = string.Join(" ", MenuItems.Where(i => i is not null));
            Game1.player.modData["FarmCafeMenuItems"] = itemsString;

            string openCloseTimeString = CafeManager.OpeningTime + " " + CafeManager.ClosingTime;
            Game1.player.modData["FarmCafeOpenCloseTimes"] = openCloseTimeString;
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
                CurrentCustomers.Clear();
            }
            else if (e.Type.StartsWith("UpdateCustomerInfo") && !Context.IsMainPlayer)
            {
                Customer c = Sync.GetAllCustomersInGame().FirstOrDefault(c => c.Name == e.Type.Split('/')[1]);
                if (c == null)
                {
                    Logger.Log("Couldn't get customer to update");
                    return;
                }
               
                switch (e.Type.Split('/')[2])
                {
                    case nameof(c.OrderItem):
                        c.OrderItem = new Object(e.ReadAs<int>(), 1).getOne();
                        break;
                    case nameof(c.TableCenterForEmote):
                        MatchCollection matches = Regex.Matches(e.ReadAs<string>(), @"\d+");
                        if (matches.Count == 2 &&
                            float.TryParse(matches[0].Value, out float x) &&
                            float.TryParse(matches[1].Value, out float y))
                        {
                            c.TableCenterForEmote = new Vector2(x, y);
                        }
                        break;
                    default:
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
                Customer c = Sync.GetAllCustomersInGame().FirstOrDefault(c => c.Name == info["name"]);
                c?.doEmote(int.Parse(info["emote"]));
            }
            else if (e.Type == "UpdateFurniture")
            {
                //CafeManagerFurnitureShouldBeUpdated = true;
            }
        }

        private static void OnRenderedWorld(object sender, RenderedWorldEventArgs e)
        {
            // get list of reserved tables with center coords
            foreach (var table in Tables)
            {
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

            //if (CafeManager.FurnitureShouldBeUpdated)
            //{
            //    CafeManager.PopulateTables(CafeLocations);
            //    CafeManager.FurnitureShouldBeUpdated = false;
            //}

            // spawn customers depending on probability logic
            //CafeManager.CheckSpawnCustomers();
        }

        private static void OnPeerConnected(object sender, PeerConnectedEventArgs e)
        {
            return;
        }

        private static void OnLocationListChanged(object sender, LocationListChangedEventArgs e)
        {
            foreach (var removed in e.Removed)
            {
                if (removed is CafeLocation)
                {
                    CafeManager.UpdateCafeLocation();
                }
            }
        }

        private static void OnFurnitureListChanged(object sender, FurnitureListChangedEventArgs e)
        {
            if (Context.IsMainPlayer && CafeLocations.Any(l => l.Equals(e.Location)))
                CafeManager.HandleFurnitureChanged(e.Added, e.Removed, e.Location);
        }
        
        private static void PrepareSolidFoundationsApi()
        {
            SfApi = ModHelper.ModRegistry.GetApi<IApi>(
                "PeacefulEnd.SolidFoundations");
            if (SfApi == null)
                throw new Exception("SF Api failed");

            SfApi.BroadcastSpecialActionTriggered += OnBuildingBroadcastTriggered;
        }

        private static void OnBuildingBroadcastTriggered(object sender, IApi.BroadcastEventArgs e)
        {
            var buildingId = e.BuildingId;
            var buildingObject = e.Building;
            var farmerObject = e.Farmer;
            var tileWhereTriggered = e.TriggerTile;
            var sentMessage = e.Message;

            if (!Context.IsWorldReady) 
                return;
            if (Game1.activeClickableMenu != null || (!Context.IsPlayerFree)) 
                return;

            if (sentMessage.ToLower() == "opencafemenu")
                OpenCafeMenu();
        }

        
        internal static void OpenCafeMenu()
        {
            if (!Context.IsMainPlayer)
                return;

            if (Game1.activeClickableMenu == null && Context.IsPlayerFree)
            {
                Logger.Log("Open menu!");
                Game1.activeClickableMenu = new CafeMenu();
            }
        }

        private static void PlaceCafeBuilding(Vector2 position)
        {
            var building = SfApi.PlaceBuilding("FarmCafeSignboard", Game1.getFarm(), position);

            if (building.Key)
            {
                Logger.Log($"building placed. message is {building.Value}");
            }
            else
            {
                Logger.Log($"building not placed. messag eis {building.Value}");
            }
        }
    }
}