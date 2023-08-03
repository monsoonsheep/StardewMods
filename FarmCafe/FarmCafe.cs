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
using xTile.Dimensions;

namespace FarmCafe
{
	/// <summary>The mod entry point.</summary>
	internal sealed class FarmCafe : Mod
	{
		internal static IMonitor Monitor;
		internal static IModHelper ModHelper;
		internal static ISolidFoundationsApi SfApi;
		internal static IManifest ModManifest;

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

			//helper.Events.Content.AssetRequested += OnAssetRequested;
			//helper.Events.Content.AssetsInvalidated += OnAssetsInvalidated;

            helper.Events.Multiplayer.ModMessageReceived += OnModMessageReceived;
            helper.Events.Multiplayer.PeerConnected += OnPeerConnected;
		}

        [EventPriority(EventPriority.High+1)]
        private static void OnDayEnding(object sender, DayEndingEventArgs e)
		{
			CustomerManager.RemoveAllCustomers();
		}

		private static void OnUpdateTicked(object sender, UpdateTickedEventArgs e)
		{
			if (!Context.IsMainPlayer && CustomerManager.ClientShouldUpdateCustomers)
			{
				CustomerManager.CurrentCustomers = CustomerManager.GetAllCustomersInGame();
				CustomerManager.ClientShouldUpdateCustomers = false;
            }
		}

        private static void OnButtonPressed(object sender, ButtonPressedEventArgs e)
		{
			switch (e.Button)
			{
				case SButton.B:
					break;
				case SButton.NumPad0:
					CustomerManager.SpawnGroupAtBus();
					break;
				case SButton.NumPad1:
					Debug.Debug_warpToBus();
					break;
				case SButton.NumPad2:
					CustomerManager.RemoveAllCustomers();
					break;
				case SButton.NumPad3:
					if (CustomerManager.CurrentGroups.Any())
					{
						CustomerManager.WarpGroup(CustomerManager.CurrentGroups.First(), Game1.getFarm(), new Point(78, 16));
					}
					break;
				case SButton.NumPad4:
					CustomerManager.Debug_ListCustomers();
					break;
				case SButton.N:
					Debug.Log(Game1.MasterPlayer.ActiveObject?.ParentSheetIndex.ToString());
					Game1.MasterPlayer.addItemToInventory(new Furniture(1220, new Vector2(0, 0)).getOne());
                    Game1.MasterPlayer.addItemToInventory(new Furniture(21, new Vector2(0, 0)).getOne());
                    break;
				case SButton.V:
                    CustomerGroup g = CustomerManager.SpawnGroup(Game1.player.currentLocation,
                        Game1.player.getTileLocationPoint() + new Point(0, -1), 1);
                    g.Members?.First()?.GoToCafe();;
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

		private static void OnModMessageReceived(object sender, ModMessageReceivedEventArgs e)
		{
            if (e.FromModID != ModManifest.UniqueID) return;

			if (e.Type == "UpdateCustomers")
            {
				CustomerManager.ClientShouldUpdateCustomers = true;
            }
			else if (e.Type == "RemoveCustomers")
            {
				CustomerManager.CurrentCustomers.Clear();
                //CustomerManager.ClientShouldUpdateCustomers = true;
            }
            else if (e.Type == "SyncTables")
			{
				var updates = e.ReadAs<Dictionary<Vector2, string>>();
				var tables = new Dictionary<Furniture, GameLocation>();
				foreach (var pair in updates)
				{
					GameLocation location = GetLocationFromName(pair.Value);
                    Furniture table = location?.GetFurnitureAt(pair.Key);
					if (table == null) continue;
					tables.Add(table, location);
                }
                TableManager.TrackedTables = tables;
            }
        }

		private static void OnGameLaunched(object sender, GameLaunchedEventArgs e)
		{
			GetSolidFoundationsApi();
		}

		private static void OnSaveLoaded(object sender, SaveLoadedEventArgs e)
		{
			TableManager.TrackedTables = new Dictionary<Furniture, GameLocation>();

            PrepareCustomerModels();
            CustomerManager.CacheBusPosition();
        }

        internal static void OnDayStarted(object sender, DayStartedEventArgs e)
		{
			CafeManager.CafeLocations = new List<GameLocation>();
            var cafeBuilding = Game1.getFarm().buildings
                .FirstOrDefault(b => b.indoors.Value is CafeLocation);

            var cafeLocation = cafeBuilding?.indoors.Value;

            if (cafeLocation == null)
            {
                Debug.Log("Didn't find cafe building on farm");
                cafeLocation = Game1.getFarm();
            }

            CafeManager.CafeLocations.Add(cafeLocation);
            Debug.Log($"Added Cafe locations {string.Join(", ", CafeManager.CafeLocations.Select(l => l.Name))}");
            
			ResetCustomers();
			CafeManager.PopulateRoutesToCafe();
            TableManager.PopulateTables();
            Messaging.SyncTables();
        }

		private static void OnSaving(object sender, EventArgs e)
		{
			// Go through all game locations and purge any customers
			//CustomerManager.RemoveAllCustomers();
		}

		private static void GetSolidFoundationsApi()
		{
			if (!ModHelper.ModRegistry.IsLoaded("PeacefulEnd.SolidFoundations"))
			{
				Debug.Log("Solid Foundations is required for this mod", LogLevel.Error);
				throw new DllNotFoundException("Solid Foundations is required for this mod");
			}

			SfApi = ModHelper.ModRegistry.GetApi<ISolidFoundationsApi>("PeacefulEnd.SolidFoundations");
			if (SfApi == null)
			{
				Debug.Log("SF Api failed", LogLevel.Error);
				throw new EntryPointNotFoundException("SF Api failed");
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
            CustomerManager.CustomerModels = new List<CustomerModel>();
            var dirs = new DirectoryInfo(Path.Combine(ModHelper.DirectoryPath, "assets", "Customers")).GetDirectories();
            foreach (var dir in dirs)
            {
                CustomerModel model = ModHelper.ModContent.Load<CustomerModel>($"assets/Customers/{dir.Name}/customer.json");
                model.TilesheetPath = ModHelper.ModContent.GetInternalAssetName($"assets/Customers/{dir.Name}/customer.png").Name;
                //Debug.Log($"Model loading: {model.ToString()}");
                //Debug.Log($"Tilesheet: {model.TilesheetPath}");
                //this SMAPI/monsoonsheep.farmcafe/assets/Customers/Catgirl/customer.png

                CustomerManager.CustomerModels.Add(model);
            }
        }

		private static void ResetCustomers()
		{
			CustomerManager.CustomerModelsInUse = new List<CustomerModel>();
			CustomerManager.CurrentCustomers = new List<Customer>();
			CustomerManager.CurrentGroups = new List<CustomerGroup>();
		}
	}
}