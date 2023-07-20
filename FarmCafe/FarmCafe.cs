using FarmCafe.Framework.Customers;
using FarmCafe.Framework.Interfaces;
using FarmCafe.Framework.Managers;
using FarmCafe.Framework.Models;
using FarmCafe.Framework.Multiplayer;
using FarmCafe.Framework.Patching;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Objects;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

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
				new GamePatch().Apply(harmony);
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
			helper.Events.GameLoop.ReturnedToTitle += OnReturnedToTitle;
			helper.Events.Input.ButtonPressed += OnButtonPressed;

			helper.Events.Content.AssetRequested += OnAssetRequested;
			helper.Events.Content.AssetsInvalidated += OnAssetsInvalidated;


            helper.Events.Multiplayer.ModMessageReceived += OnModMessageReceived;
            helper.Events.Multiplayer.PeerConnected += OnPeerConnected;
		}

		private static void OnButtonPressed(object sender, ButtonPressedEventArgs e)
		{
			switch (e.Button)
			{
				case SButton.B:
					break;
				case SButton.NumPad0:
					CustomerManager.SpawnGroupBus();
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
				case SButton.X:
					var fiber = new StardewValley.Object(746, 1);
					fiber.DisplayName = "Fiber";
					fiber.Name = "Fiber";
					var msg = new HUDMessage(null, 1, false, new Color(0.1f, 0.5f, 0.5f), fiber);
					msg.add = true;
					msg.Message = "Fiber";
					
					msg.timeLeft = 500;
					
                    Game1.addHUDMessage(msg);
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
                List<string> names = e.ReadAs<CustomerUpdate>().names;
				CustomerManager.UpdateCustomerList(names);
            }
			else if (e.Type == "SyncTables")
			{
				var update = e.ReadAs<List<Vector2>>();
				List<Furniture> tables = new List<Furniture>();
				foreach (var pos in update)
				{
					Furniture table = Game1.getFarm().GetFurnitureAt(pos);
					if (table == null) continue;
					tables.Add(table);
                }
                TableManager.TrackedTables = tables;
            }
        }

		private static void OnGameLaunched(object sender, GameLaunchedEventArgs e)
		{
			//GetSolidFoundationsApi();
		}

		private static void OnSaveLoaded(object sender, SaveLoadedEventArgs e)
		{
			//PrepareCafeBuilding();
			PrepareCustomerManager();

			//PlaceCafeBuilding(new Vector2(48, 13));
		}

		internal static void OnDayStarted(object sender, DayStartedEventArgs e)
		{
			// Get all the tables on the farm and register them as Table objects
			TableManager.PopulateTables();
		}

		private static void OnSaving(object sender, EventArgs e)
		{
			// Go through all game locations and purge any customers
			CustomerManager.RemoveAllCustomers();
		}

		private static void OnReturnedToTitle(object sender, ReturnedToTitleEventArgs e)
		{
			CustomerManager.RemoveAllCustomers();
		}
		private static void OnAssetRequested(object sender, AssetRequestedEventArgs e)
		{
			//if (e.Name.StartsWith("Characters\\Dialogue\\Customer_"))
			//{
			//       var dialogue = new Dictionary<string, string> { { "Farm_Entry", "Hi!" } };

			//       Debug.Log("Loading dialogue for customer", LogLevel.Warn);
			//       e.LoadFrom(
			//               () => dialogue,
			//               AssetLoadPriority.Exclusive);
			//   }

			if (e.Name.Name.ToLower().Contains("customer") && e.DataType == typeof(Texture2D))
			{
				Debug.Log($"Asset requested {e.Name.Name}");
			}

		}

		private static void OnAssetsInvalidated(object sender, AssetsInvalidatedEventArgs e)
		{
			Debug.Log($"Invalidated!! {e.ToString()}", LogLevel.Warn);

			foreach (var name in e.Names)
			{
				Debug.Log(name.Name);
			}
		}

		private static void PrepareCafeBuilding()
		{
			if (!ModHelper.ModRegistry.IsLoaded("monsoonsheep.FarmCafe"))
			{
				Debug.Log("Cafe Building not found", LogLevel.Error);
				throw new Exception("Cafe Building not found");
			}
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
			var building = SfApi.PlaceBuilding("FarmCafeBuilding", Game1.getFarm(), position);

			if (building.Key)
			{
				Debug.Log($"building placed. message is {building.Value}");
			}
			else
			{
				Debug.Log($"buildlng not placed. messag eis {building.Value}");
			}
		}

		private static void PrepareCustomerManager()
		{
			CustomerManager.BusStop = Game1.getLocationFromName("BusStop");
			CustomerManager.CustomerModelsInUse = new List<CustomerModel>();
			CustomerManager.CurrentCustomers = new List<Customer>();
			CustomerManager.CurrentGroups = new List<CustomerGroup>();
			TableManager.TrackedTables = new List<Furniture>();
			Messaging.SyncTables();
			CustomerManager.CacheBusWarpsToFarm();
			CustomerManager.CacheBusPosition();

			CustomerManager.CustomerModels = new List<CustomerModel>();
			var dirs = new DirectoryInfo(Path.Combine(ModHelper.DirectoryPath, "assets", "Customers")).GetDirectories();
			//Debug.Log(dirs.FirstOrDefault()?.Name);
			foreach (var dir in dirs)
			{
				CustomerModel model = ModHelper.ModContent.Load<CustomerModel>(Path.Combine("assets", "Customers", dir.Name, "customer.json"));
				model.TilesheetPath = ModHelper.ModContent.GetInternalAssetName(Path.Combine("assets", "Customers", dir.Name, "customer.png")).Name;
				//Debug.Log($"Model loading: {model.ToString()}");
				//Debug.Log($"\tTilesheet: {model.TilesheetPath}");


				CustomerManager.CustomerModels.Add(model);
			}
		}
	}
}