using FarmCafe.Framework.Interfaces;
using FarmCafe.Framework.Managers;
using FarmCafe.Framework.Models;
using FarmCafe.Framework.Patching;
using FarmCafe.Locations;
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
using System.Text.RegularExpressions;
using FarmCafe.Framework.Characters;
using FarmCafe.Framework.UI;
using Microsoft.VisualBasic.FileIO;
using Sickhead.Engine.Util;
using StardewValley.Menus;
using xTile.Dimensions;
using SolidFoundations.Framework.Interfaces.Internal;

namespace FarmCafe
{
    /// <summary>The mod entry point.</summary>
    internal sealed class FarmCafe : Mod
    {
        internal static IMonitor Monitor;
        internal static IModHelper ModHelper;
        internal static IApi SfApi;
        internal static IManifest ModManifest;

        internal static CafeManager CafeManager;
        internal static TableManager TableManager;

        // To be synced in multiplayer
        internal static List<GameLocation> CafeLocations = new();
        internal static IList<Item> MenuItems = new List<Item>(new Item[27]);
        internal static IList<Item> RecentlyAddedMenuItems = new List<Item>(new Item[9]);
        internal static List<Customer> CurrentCustomers = new List<Customer>();
        internal static NPC HelperNpc;
        internal static Dictionary<Furniture, GameLocation> TrackedTables = new Dictionary<Furniture, GameLocation>();

        internal static bool ClientShouldUpdateCustomers = false;

        /*********
        ** Public methods
        *********/
        /// <summary>The mod entry point, called after the mod is first loaded.</summary>
        /// <param name="helper">Provides simplified APIs for writing mods.</param>
        public override void Entry(IModHelper helper)
        {
            Monitor = base.Monitor;
            ModHelper = helper;
            Debug.Monitor = Monitor;
            ModManifest = base.ModManifest;
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
                Debug.Log($"Couldn't patch methods - {e}", LogLevel.Error);
                return;
            }

            helper.Events.GameLoop.GameLaunched += OnGameLaunched;
            helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
            helper.Events.GameLoop.DayStarted += OnDayStarted;
            helper.Events.GameLoop.DayEnding += OnDayEnding;
            helper.Events.GameLoop.Saving += OnSaving;

            
            helper.Events.GameLoop.TimeChanged += OnTimeChanged;
            helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
            helper.Events.Input.ButtonPressed += OnButtonPressed;
            helper.Events.Content.AssetRequested += OnAssetRequested;
            

            // Multiplayer
            helper.Events.Multiplayer.PeerConnected += OnPeerConnected;
            helper.Events.Multiplayer.ModMessageReceived += OnModMessageReceived;
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
            if (Context.IsMainPlayer)
            {
                MenuItems = new List<Item>(new Item[27]);
                RecentlyAddedMenuItems = new List<Item>(new Item[9]);
                MenuItems[0] = new StardewValley.Object(746, 1).getOne();
                RecentlyAddedMenuItems[0] = new StardewValley.Object(746, 1).getOne();

                TableManager = new TableManager(ref TrackedTables);
                CafeManager = new CafeManager(TableManager, CafeLocations, MenuItems, CurrentCustomers, null);
                PrepareCustomerModels();
                CafeManager.CacheBusPosition();
            }
            else
            {
                CurrentCustomers = CafeManager.GetAllCustomersInGame();
                // tables, menu items are updated by the host with a message
            }
            // Multiplayer clients get updated with the state of managers
        }

        private void OnDayStarted(object sender, DayStartedEventArgs e)
        {
            if (!CafeLocations.Any(l => l is CafeLocation))
            {
                GameLocation cafeloc = LookForCafeLocation();
                if (cafeloc != null)
                {
                    CafeLocations.Add(cafeloc);
                }
                else
                    CafeLocations = new List<GameLocation>() { Game1.getFarm() };
            }

            if (Context.IsMainPlayer)
                CafeManager.PopulateRoutesToCafe();

            Debug.Log($"Cafe locations are {string.Join(", ", CafeLocations.Select(l => l.Name))}");

            if (Context.IsMainPlayer)
            {
                CafeManager.ResetCustomers();
                TableManager.PopulateTables(CafeLocations);
            }
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
            return;
        }

        private static void OnModMessageReceived(object sender, ModMessageReceivedEventArgs e)
        {
            if (e.FromModID != ModManifest.UniqueID) return;

            if (e.Type == "UpdateCustomers")
            {
                ClientShouldUpdateCustomers = true;
            }
            else if (e.Type == "RemoveCustomers")
            {
                CurrentCustomers.Clear();
                //CustomerManager.ClientShouldUpdateCustomers = true;
            }
            else if (e.Type.StartsWith("UpdateCustomerInfo") && !Context.IsMainPlayer)
            {
                Customer c = CafeManager.GetAllCustomersInGame().FirstOrDefault(c => c.Name == e.Type.Split('/')[1]);
                if (c == null)
                {
                    Debug.Log("Couldn't get customer to update");
                    return;
                }
               
                switch (e.Type.Split('/')[2])
                {
                    case nameof(c.OrderItem):
                        c.OrderItem = new StardewValley.Object(e.ReadAs<int>(), 1).getOne();
                        break;
                    case nameof(c.tableCenterForEmote):
                        MatchCollection matches = Regex.Matches(e.ReadAs<string>(), @"\d+");
                        if (matches.Count == 2 &&
                            float.TryParse(matches[0].Value, out float x) &&
                            float.TryParse(matches[1].Value, out float y))
                        {
                            c.tableCenterForEmote = new Vector2(x, y);
                        }
                        break;
                    default:
                        break;
                }
            }
            else if (e.Type == "SyncTables" && !Context.IsMainPlayer)
            {
                //TrackedTables.Clear();
                //foreach (var pair in e.ReadAs<Dictionary<Vector2, string>>())
                //{
                //    GameLocation loc = GetLocationFromName(pair.Value);
                //    Furniture furniture = loc?.GetFurnitureAt(pair.Key);
                //    if (furniture == null) continue;
                //    TrackedTables.Add(furniture, loc);
                //}
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
                    Furniture table = who.currentLocation.GetFurnitureAt(new Vector2(x, y));
                    CafeManager.FarmerClickTable(who, table);
                }
            }
            else if (e.Type == "CustomerDoEmote" && !Context.IsMainPlayer)
            {
                var info = e.ReadAs<Dictionary<string, string>>();
                Customer c = CafeManager.GetAllCustomersInGame().FirstOrDefault(c => c.Name == info["name"]);
                c?.doEmote(int.Parse(info["emote"]));
            }
            else if (e.Type == "UpdateFurniture")
            {
                TableManager.FurnitureShouldBeUpdated = true;
            }
        }
        
        private static void OnUpdateTicked(object sender, UpdateTickedEventArgs e)
        {
            if (!Context.IsMainPlayer && ClientShouldUpdateCustomers)
            {
                CurrentCustomers = CafeManager.GetAllCustomersInGame();
                ClientShouldUpdateCustomers = false;
            }
        }

        private static void OnTimeChanged(object sender, TimeChangedEventArgs e)
        {
            if (!Context.IsMainPlayer)
                return;

            if (TableManager.FurnitureShouldBeUpdated)
            {
                TableManager.PopulateTables(CafeLocations);
                TableManager.FurnitureShouldBeUpdated = false;
            }
        }

        private static void OnButtonPressed(object sender, ButtonPressedEventArgs e)
        {
            if (!Context.IsMainPlayer || !Context.CanPlayerMove)
                return;

            switch (e.Button)
            {
                case SButton.B:
                    break;
                case SButton.NumPad0:
                    CafeManager.SpawnGroupAtBus();
                    break;
                case SButton.NumPad1:
                    Debug.Debug_warpToBus();
                    break;
                case SButton.NumPad2:
                    CafeManager.RemoveAllCustomers();
                    break;
                case SButton.NumPad3:
                    if (CafeManager.CurrentGroups.Any())
                    {
                        CafeManager.WarpGroup(CafeManager.CurrentGroups.First(), Game1.getFarm(), new Point(78, 16));
                    }

                    break;
                case SButton.NumPad4:
                    Game1.activeClickableMenu = new CarpenterMenu();
                    CafeManager.Debug_ListCustomers();
                    break;
                case SButton.NumPad5:
                    OpenCafeMenu();
                    //NPC helper = Game1.getCharacterFromName("Sebastian");
                    //helper.clearSchedule();
                    //helper.ignoreScheduleToday = true;
                    //Game1.warpCharacter(helper, "BusStop", CustomerManager.BusPosition);
                    //helper.HeadTowards(CafeManager.CafeLocations.First(), new Point(12, 18), 2);
                    //helper.eventActor = true;
                    break;
                case SButton.NumPad6:
                    Debug.Log(string.Join(", ", MenuItems.Select(i => i.DisplayName)));
                    break;
                case SButton.M:
                    Debug.Log("Breaking");
                    break;
                case SButton.N:
                    Debug.Log(Game1.MasterPlayer.ActiveObject?.ParentSheetIndex.ToString());
                    Game1.MasterPlayer.addItemToInventory(new Furniture(1220, new Vector2(0, 0)).getOne());
                    Game1.MasterPlayer.addItemToInventory(new Furniture(21, new Vector2(0, 0)).getOne());
                    break;
                case SButton.V:
                    CustomerGroup g = CafeManager.SpawnGroup(Game1.player.currentLocation,
                        Game1.player.getTileLocationPoint() + new Point(0, -1), 1);
                    g?.Members?.First()?.GoToSeat();
                    break;
                default:
                    return;
            }
        }

        private static void OnPeerConnected(object sender, PeerConnectedEventArgs e)
        {
            return;
        }

        private static void OnAssetRequested(object sender, AssetRequestedEventArgs e)
        {
            if (e.Name.IsEquivalentTo("Characters/Schedules/Sebastian"))
            {
                Debug.Log("Sebiastian schedule");
                e.LoadFrom(() => new Dictionary<string, string>(), AssetLoadPriority.Exclusive);
            }
        }


        private static void PrepareSolidFoundationsApi()
        {
            SfApi = ModHelper.ModRegistry.GetApi<SolidFoundations.Framework.Interfaces.Internal.IApi>(
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
            if (!Context.IsWorldReady) return;
            if (Game1.activeClickableMenu != null || (!Context.IsPlayerFree)) return;

            if (sentMessage.ToLower() == "opencafemenu")
            {
                OpenCafeMenu();
            }
        }

        internal static GameLocation GetLocationFromName(string name)
        {
            return Game1.getLocationFromName(name) ?? CafeLocations.FirstOrDefault(a => a.Name == name);
        }

        private static GameLocation LookForCafeLocation()
        {
            return Game1.getFarm().buildings
                .FirstOrDefault(b => b.indoors.Value is CafeLocation)
                ?.indoors.Value;
        }

        internal static void OpenCafeMenu()
        {
            if (!Context.IsMainPlayer)
                return;

            if (Game1.activeClickableMenu == null && Context.IsPlayerFree)
            {
                Debug.Log("Open menu!");
                Game1.activeClickableMenu = new CafeMenu(ref MenuItems, ref RecentlyAddedMenuItems, CafeManager.AddToMenu, CafeManager.RemoveFromMenu);
            }
        }

        private static void PlaceCafeBuilding(Vector2 position)
        {
            var building = SfApi.PlaceBuilding("FarmCafeSignboard", Game1.getFarm(), position);

            if (building.Key)
            {
                Debug.Log($"building placed. message is {building.Value}");
            }
            else
            {
                Debug.Log($"building not placed. messag eis {building.Value}");
            }
        }


        private static void PrepareCustomerModels()
        {
            CafeManager.CustomerModels = new List<CustomerModel>();
            var dirs = new DirectoryInfo(Path.Combine(ModHelper.DirectoryPath, "assets", "Customers")).GetDirectories();
            foreach (var dir in dirs)
            {
                CustomerModel model =
                    ModHelper.ModContent.Load<CustomerModel>($"assets/Customers/{dir.Name}/customer.json");
                model.TilesheetPath = ModHelper.ModContent
                    .GetInternalAssetName($"assets/Customers/{dir.Name}/customer.png").Name;
                //Debug.Log($"Model loading: {model.ToString()}");
                //Debug.Log($"Tilesheet: {model.TilesheetPath}");
                //this SMAPI/monsoonsheep.farmcafe/assets/Customers/Catgirl/customer.png

                CafeManager.CustomerModels.Add(model);
            }
        }


    }
}