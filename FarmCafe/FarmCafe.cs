using FarmCafe.Framework.Interfaces;
using FarmCafe.Framework.Managers;
using FarmCafe.Framework.Models;
using FarmCafe.Framework.Multiplayer;
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
using FarmCafe.Framework.Characters;
using FarmCafe.Framework.UI;
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
		internal IApi SfApi;
		internal static IManifest ModManifest;

        internal static CafeManager cafeManager;
        internal static TableManager tableManager;

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

			
			//helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
			helper.Events.GameLoop.GameLaunched += OnGameLaunched;
			helper.Events.GameLoop.DayStarted += OnDayStarted;
			helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
			helper.Events.GameLoop.Saving += OnSaving;
			helper.Events.Input.ButtonPressed += OnButtonPressed;
			helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
			helper.Events.GameLoop.DayEnding += OnDayEnding;

			helper.Events.Content.AssetRequested += OnAssetRequested;
			//helper.Events.Content.AssetsInvalidated += OnAssetsInvalidated;

			helper.Events.Multiplayer.ModMessageReceived += OnModMessageReceived;
            helper.Events.Multiplayer.PeerConnected += OnPeerConnected;
		}

        [EventPriority(EventPriority.High+1)]
        private void OnDayEnding(object sender, DayEndingEventArgs e)
		{
            cafeManager.RemoveAllCustomers();
		}

		private void OnUpdateTicked(object sender, UpdateTickedEventArgs e)
		{
			if (!Context.IsMainPlayer && cafeManager.ClientShouldUpdateCustomers)
			{
				cafeManager.CurrentCustomers = cafeManager.GetAllCustomersInGame();
				cafeManager.ClientShouldUpdateCustomers = false;
            }
		}

        private void OnButtonPressed(object sender, ButtonPressedEventArgs e)
		{
			switch (e.Button)
			{
				case SButton.B:
					break;
				case SButton.NumPad0:
                    cafeManager.SpawnGroupAtBus();
					break;
				case SButton.NumPad1:
					Debug.Debug_warpToBus();
					break;
				case SButton.NumPad2:
					cafeManager.RemoveAllCustomers();
					break;
				case SButton.NumPad3:
					if (cafeManager.CurrentGroups.Any())
					{
						cafeManager.WarpGroup(cafeManager.CurrentGroups.First(), Game1.getFarm(), new Point(78, 16));
					}
					break;
				case SButton.NumPad4:
                    Game1.activeClickableMenu = new CarpenterMenu();
					cafeManager.Debug_ListCustomers();
					break;
				case SButton.NumPad5:
					OpenCafeMenu();
                    //var signboardPos =
                    //    Game1.getFarm().buildings.FirstOrDefault(b => b.indoors.Value is CafeLocation).humanDoor.Value -
                    //    new Point(-1, 1);

                    //SfApi.PlaceBuilding("FarmCafeSignboard", Game1.getFarm(), signboardPos.ToVector2());
                    //NPC helper = Game1.getCharacterFromName("Sebastian");
                    //helper.clearSchedule();
                    //helper.ignoreScheduleToday = true;
                    //Game1.warpCharacter(helper, "BusStop", CustomerManager.BusPosition);
                    //helper.HeadTowards(CafeManager.CafeLocations.First(), new Point(12, 18), 2);
                    //helper.eventActor = true;
                    break;
				case SButton.NumPad6:
					Debug.Log(string.Join(", ", cafeManager.MenuItems.Select(i => i.DisplayName)));
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
                    CustomerGroup g = cafeManager.SpawnGroup(Game1.player.currentLocation,
                        Game1.player.getTileLocationPoint() + new Point(0, -1), 1);
                    g?.Members?.First()?.GoToSeat();;
					//Debug.Log(Game1.getLocationFromName("FarmCafe.CafeBuilding")?.Name);
                    //               foreach (var building in Game1.getFarm().buildings)
					//{
					//	if (building.indoors.Value is not null)
					//	{
     //                       Debug.Log($"Indoor: {building.indoors?.Value?.Name}");
     //                       foreach (var f in building.indoors.Value.furniture)
					//		{
					//			Debug.Log($"Furniture {f.Name}");
					//		}
					//	}
					//}
					//Debug.Log($"{Game1.MasterPlayer.currentLocation.Name} location, {Game1.MasterPlayer.currentLocation is CafeLocation}!");
					//Debug.Log($"contains? {Game1.locations.Contains(Game1.MasterPlayer.currentLocation)}");
					//Messaging.AddCustomerGroup(CustomerManager.CurrentGroups.First());
                    //CustomerManager.populateRoutesToCafe();
                    break;
				default:
					return;
			}
		}

		private static void OnPeerConnected(object sender, PeerConnectedEventArgs e)
		{

		}

		private void OnModMessageReceived(object sender, ModMessageReceivedEventArgs e)
		{
            if (e.FromModID != ModManifest.UniqueID) return;

			if (e.Type == "UpdateCustomers")
            {
				cafeManager.ClientShouldUpdateCustomers = true;
            }
			else if (e.Type == "RemoveCustomers")
            {
				cafeManager.CurrentCustomers.Clear();
                //CustomerManager.ClientShouldUpdateCustomers = true;
            }
            else if (e.Type == "SyncTables")
			{
				var updates = e.ReadAs<Dictionary<Vector2, string>>();
				var tables = new Dictionary<Furniture, GameLocation>();
				foreach (var pair in updates)
				{
					GameLocation location = cafeManager.GetLocationFromName(pair.Value);
                    Furniture table = location?.GetFurnitureAt(pair.Key);
					if (table == null) continue;
					tables.Add(table, location);
                }
                tableManager.TrackedTables = tables;
            }
        }

		private void OnGameLaunched(object sender, GameLaunchedEventArgs e)
		{
			GetSolidFoundationsApi();
            tableManager = new TableManager();
            cafeManager = new CafeManager(tableManager);
		}

		private void OnSaveLoaded(object sender, SaveLoadedEventArgs e)
        {
            tableManager.TrackedTables = new Dictionary<Furniture, GameLocation>();

            PrepareCustomerModels();
            cafeManager.CacheBusPosition();
        }

        internal void OnDayStarted(object sender, DayStartedEventArgs e)
		{
            cafeManager.CafeLocations = new List<GameLocation>();
            var cafeBuilding = Game1.getFarm().buildings
                .FirstOrDefault(b => b.indoors.Value is CafeLocation);

            var cafeLocation = cafeBuilding?.indoors.Value;

            if (cafeLocation == null)
            {
                Debug.Log("Didn't find cafe building on farm");
                cafeLocation = Game1.getFarm();
            }

            cafeManager.CafeLocations.Add(cafeLocation);
            Debug.Log($"Added Cafe locations {string.Join(", ", cafeManager.CafeLocations.Select(l => l.Name))}");
            
			tableManager.CafeLocations = cafeManager.CafeLocations;

			ResetCustomers();
            cafeManager.PopulateRoutesToCafe();
            tableManager.PopulateTables();
            Messaging.SyncTables();

            //cafeManager.MenuItems = new MenuList();
            //{
            //    new StardewValley.Object(746, 1).getOne();
            //};
        }

		private static void OnSaving(object sender, EventArgs e)
		{
			// Go through all game locations and purge any customers
			//CustomerManager.RemoveAllCustomers();
		}

        private static void OnAssetRequested(object sender, AssetRequestedEventArgs e)
        {
            if (e.Name.IsEquivalentTo("Characters/Schedules/Sebastian"))
            {
				Debug.Log("Sebiastian schedule");
				e.LoadFrom(() => new Dictionary<string, string>(), AssetLoadPriority.Exclusive);
                
            }
        }

		/// <summary>
		/// Access and save the API from the Solid Foundations mod
		/// </summary>
		/// <exception cref="Exception"></exception>
		private void GetSolidFoundationsApi()
		{
            SfApi = ModHelper.ModRegistry.GetApi<SolidFoundations.Framework.Interfaces.Internal.IApi>("PeacefulEnd.SolidFoundations");
            if (SfApi == null)
                throw new Exception("SF Api failed");
			
            SfApi.BroadcastSpecialActionTriggered += OnBuildingBroadcastTriggered;
        }

		/// <summary>
		/// The solid foundations API fires an Event called BroadcastSpecialActionTriggered, and this method is added to it
		/// </summary>
		/// <param name="sender">The object that sent the event</param>
		/// <param name="e">Event arguments sent by the event</param>
        private void OnBuildingBroadcastTriggered(object sender, IApi.BroadcastEventArgs e)
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

		/// <summary>
		/// Open the cafe menu
		/// </summary>
        internal void OpenCafeMenu()
        {
            Debug.Log("Open menu!");
            Game1.activeClickableMenu = new CafeMenu(cafeManager.MenuItems, cafeManager.RecentlyAddedMenuItems);
        }

		/// <summary>
		/// Debug: build the cafe building
		/// </summary>
		/// <param name="position"></param>
		private void PlaceCafeBuilding(Vector2 position)
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

		/// <summary>
		/// Scan customer models in the asset directory and create CustomerModel instances
		/// </summary>
		private void PrepareCustomerModels()
		{
            cafeManager.CustomerModels = new List<CustomerModel>();
            var dirs = new DirectoryInfo(Path.Combine(ModHelper.DirectoryPath, "assets", "Customers")).GetDirectories();
            foreach (var dir in dirs)
            {
                CustomerModel model = ModHelper.ModContent.Load<CustomerModel>($"assets/Customers/{dir.Name}/customer.json");
                model.TilesheetPath = ModHelper.ModContent.GetInternalAssetName($"assets/Customers/{dir.Name}/customer.png").Name;
                //Debug.Log($"Model loading: {model.ToString()}");
                //Debug.Log($"Tilesheet: {model.TilesheetPath}");
                //this SMAPI/monsoonsheep.farmcafe/assets/Customers/Catgirl/customer.png

                cafeManager.CustomerModels.Add(model);
            }
        }

		/// <summary>
		/// TODO
		/// </summary>
		private void ResetCustomers()
		{
			cafeManager.CustomerModelsInUse = new List<CustomerModel>();
            cafeManager.CurrentCustomers = new List<Customer>();
            cafeManager.CurrentGroups = new List<CustomerGroup>();
		}

	}
}