using HarmonyLib;
using Microsoft.Xna.Framework.Graphics;
using MyCafe.Framework.Objects;
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
using MyCafe.Framework;
using MyCafe.Framework.Managers;
using MyCafe.Patching;
using MyCafe.Framework.Customers;
using VisitorFramework.Framework.UI;
using MyCafe.Framework.Interfaces;

namespace MyCafe
{
    public class ModEntry : Mod
    {
        internal new static IMonitor Monitor;
        internal static IModHelper ModHelper;
        internal new static IManifest ModManifest;

        private CafeManager cafeManager;
        private AssetManager assetManager;
        private CustomerManager customerManager;
        private TableManager tableManager;
        private MenuManager menuManager;

        /// <inheritdoc/>
        public override void Entry(IModHelper helper)
        {
            Monitor = base.Monitor;
            ModHelper = helper;
            ModManifest = base.ModManifest;
            Log.Monitor = Monitor;
            I18n.Init(helper.Translation);
            Config.LoadedConfig = helper.ReadConfig<ConfigModel>();

            // Harmony patches
            //try
            //{
            //    var harmony = new Harmony(ModManifest.UniqueID);
            //    new List<PatchCollection>
            //    {
            //        new CharacterPatches(), new GameLocationPatches(), new FurniturePatches()
            //    }.ForEach(l => l.ApplyAll(harmony));
            //}
            //catch (Exception e)
            //{
            //    Log.Debug($"Couldn't patch methods - {e}", LogLevel.Error);
            //    return;
            //}
            helper.Events.GameLoop.GameLaunched += OnGameLaunched;
            helper.Events.Content.AssetRequested += AssetManager.OnAssetRequested;
            helper.Events.Content.AssetReady += AssetManager.OnAssetReady;
        }

        private void OnGameLaunched(object sender, GameLaunchedEventArgs e)
        {
            //GameLocation.RegisterTileAction(ModKeys.SIGNBOARD_BUILDING_CLICK_EVENT_KEY, menuManager.OpenCafeMenuTileAction);
            Config.Initialize();
            AssetManager.LoadContentPacks(ModHelper);

            ModHelper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
            ModHelper.Events.GameLoop.ReturnedToTitle += OnReturnedToTitle;
            ModHelper.Events.Multiplayer.ModMessageReceived += Sync.OnModMessageReceived;
#if DEBUG
            ModHelper.Events.Input.ButtonPressed += Debug.ButtonPress;
#endif
        }

        private void OnSaveLoaded(object sender, SaveLoadedEventArgs e)
        {
            if (!Context.IsMainPlayer)
                return;
            
            cafeManager = new CafeManager();
            assetManager = AssetManager.Instance;
            customerManager = CustomerManager.Instance;
            tableManager = TableManager.Instance;
            menuManager = MenuManager.Instance;

            assetManager.LoadValuesFromModData();

            ModHelper.Events.GameLoop.DayStarted += cafeManager.DayUpdate;
            ModHelper.Events.GameLoop.TimeChanged += cafeManager.OnTimeChanged;
            ModHelper.Events.World.FurnitureListChanged += tableManager.OnFurnitureListChanged;
            ModHelper.Events.Display.RenderedWorld += tableManager.OnRenderedWorld;
            ModHelper.Events.Multiplayer.PeerConnected += Sync.OnPeerConnected;
        }

        private void OnReturnedToTitle(object sender, ReturnedToTitleEventArgs e)
        {
            ModHelper.Events.GameLoop.DayStarted -= cafeManager.DayUpdate;
            ModHelper.Events.GameLoop.TimeChanged -= cafeManager.OnTimeChanged;
            ModHelper.Events.World.FurnitureListChanged -= tableManager.OnFurnitureListChanged;
            ModHelper.Events.Display.RenderedWorld -= tableManager.OnRenderedWorld;
            ModHelper.Events.Multiplayer.PeerConnected -= Sync.OnPeerConnected;
        }
    }
}