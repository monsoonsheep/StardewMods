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
using FarmCafe.Framework.Objects;
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
        internal new static IMonitor Monitor;
        internal static IModHelper ModHelper;
        internal static IApi SfApi;
        internal new static IManifest ModManifest;

        internal static CafeManager CafeManager;
        internal static TableManager TableManager;

        // To be synced in multiplayer
        internal static List<GameLocation> CafeLocations = new();
        internal static IList<Item> MenuItems = new List<Item>(new Item[27]);
        internal static IList<Item> RecentlyAddedMenuItems = new List<Item>(new Item[9]);
        internal static List<Customer> CurrentCustomers = new List<Customer>();
        internal static NPC HelperNpc;
        internal static List<Table> Tables = new();

        internal static bool ClientShouldUpdateCustomers = false;

        /// <inheritdoc/>
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

            helper.Events.World.LocationListChanged += OnLocationListChanged;
            helper.Events.World.FurnitureListChanged += OnFurnitureListChanged;

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
            Tables = new();
            CafeLocations = new();
            CurrentCustomers = new();

            if (Context.IsMainPlayer)
            {
                MenuItems = new List<Item>(new Item[27]);
                RecentlyAddedMenuItems = new List<Item>(new Item[9]);
                MenuItems[0] = new StardewValley.Object(746, 1).getOne();
                RecentlyAddedMenuItems[0] = new StardewValley.Object(746, 1).getOne();

                TableManager = new TableManager(ref Tables);
                CafeManager = new CafeManager(ref TableManager, ref CafeLocations, ref MenuItems, ref CurrentCustomers, null);

                CafeManager.openingTime = 0900;
                CafeManager.closingTime = 2100;

                PrepareCustomerModels();
            }
            else
            {
                CurrentCustomers = CafeManager.GetAllCustomersInGame();
                // tables, menu items are updated by the host with a message
            }
            // Multiplayer clients get updated with the state of managers
        }

        private static void OnDayStarted(object sender, DayStartedEventArgs e)
        {
            UpdateCafeLocation();
            if (CafeLocations.Count == 0)
            {
                CafeLocations.Add(Game1.getFarm());
            }
            else
            {
                CafeLocations.OfType<CafeLocation>().FirstOrDefault()?.PopulateMapTables();
            }
            Debug.Log($"Cafe locations are {string.Join(", ", CafeLocations.Select(l => l.Name))}");

            if (Context.IsMainPlayer)
            {
                CafeManager.PopulateRoutesToCafe();
                TableManager.PopulateTables(CafeLocations);
                CafeManager.LastTimeCustomersArrived = CafeManager.openingTime;
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
                    Table table = TableManager.GetTableAt(who.currentLocation, new Vector2(x, y));
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

            // spawn customers depending on probability logic
            CafeManager.CheckSpawnCustomers();
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
                    Debug.Debug_ListCustomers();
                    break;
                case SButton.NumPad5:
                    CafeLocations.OfType<CafeLocation>()?.FirstOrDefault()?.PopulateMapTables();
                    //OpenCafeMenu();
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

        private static void OnLocationListChanged(object sender, LocationListChangedEventArgs e)
        {
            foreach (var removed in e.Removed)
            {
                if (removed is CafeLocation)
                {
                    UpdateCafeLocation();
                }
            }
        }

        private static void OnFurnitureListChanged(object sender, FurnitureListChangedEventArgs e)
        {
            if (!Context.IsMainPlayer || !CafeLocations.Any(l => l.Equals(e.Location)))
                return;

            foreach (var removed in e.Removed)
            {
                if (IsChair(removed))
                {
                    FurnitureChair trackedChair = FarmCafe.Tables
                        .OfType<FurnitureTable>()
                        .SelectMany(t => t.Seats)
                        .OfType<FurnitureChair>()
                        .FirstOrDefault(seat => seat.Position == removed.TileLocation && seat.Table.CurrentLocation.Equals(e.Location));

                    if (trackedChair?.Table is not FurnitureTable table)
                        continue;

                    if (table.IsReserved)
                        Debug.Log("Removed a chair but the table was reserved");

                    table.RemoveChair(removed);
                }
                else if (IsTable(removed))
                {
                    FurnitureTable trackedTable = FarmCafe.IsTableTracked(removed, e.Location);

                    if (trackedTable != null)
                    {
                        FarmCafe.TableManager.RemoveTable(trackedTable);
                    }
                }
            }

            foreach (var added in e.Added)
            {
                if (IsChair(added))
                {
                    // Get position of table in front of the chair
                    Vector2 tablePos = added.TileLocation + (DirectionIntToDirectionVector(added.currentRotation.Value) * new Vector2(1, -1));

                    // Get table Furniture object
                    Furniture facingFurniture = e.Location.GetFurnitureAt(tablePos);

                    if (facingFurniture == null ||
                        !IsTable(facingFurniture) ||
                        facingFurniture
                            .getBoundingBox(facingFurniture.TileLocation)
                            .Intersects(added.boundingBox.Value)) // if chair was placed on top of the table
                    {
                        continue;
                    }

                    FurnitureTable newTable = TryAddFurnitureTable(facingFurniture, e.Location);
                    newTable?.AddChair(added);
                }
                else if (IsTable(added))
                {
                    TryAddFurnitureTable(added, e.Location);
                }
            }
        }
        
        internal static FurnitureTable TryAddFurnitureTable(Furniture table, GameLocation location)
        {
            FurnitureTable trackedTable = IsTableTracked(table, location);

            if (trackedTable == null)
            {
                trackedTable = new FurnitureTable(table, location)
                {
                    CurrentLocation = location
                };
                if (TableManager.TryAddTable(trackedTable))
                    return trackedTable;
                else
                    return null;
                
            }

            return trackedTable;
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

        private static bool UpdateCafeLocation()
        {
            CafeLocation cafeloc = LookForCafeLocation();
            CafeLocation existingCafeLocation = CafeLocations.OfType<CafeLocation>().FirstOrDefault();

            if (cafeloc == null)
            {
                return false;
            }
            if (existingCafeLocation == null)
            {
                CafeLocations.Add(cafeloc);
            }
            else if (!cafeloc.Equals(existingCafeLocation))
            {
                CafeLocations.Remove(existingCafeLocation);
                CafeLocations.Add(cafeloc);
            }

            return true;
        }

        private static CafeLocation LookForCafeLocation()
        {
            return Game1.getFarm().buildings
                .FirstOrDefault(b => b.indoors.Value is CafeLocation)
                ?.indoors.Value as CafeLocation;
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

        internal static CafeLocation GetCafeLocation()
        {
            return CafeLocations.OfType<CafeLocation>().FirstOrDefault();
        }

        internal static FurnitureTable IsTableTracked(Furniture table, GameLocation location)
        {
            return Tables
                .OfType<FurnitureTable>().FirstOrDefault(t => t.CurrentLocation.Equals(location) && t.Position == table.TileLocation);
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