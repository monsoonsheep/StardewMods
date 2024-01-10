using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MyCafe.Interfaces;
using MyCafe.Managers;
using MyCafe.Patching;
using Netcode;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace MyCafe;

public class Mod : StardewModdingAPI.Mod
{
    internal new static IMonitor Monitor;
    internal static IModHelper ModHelper;
    internal new static IManifest ModManifest;

    internal static NetRef<Cafe> NetCafe = new NetRef<Cafe>(new Cafe());
    internal static Cafe Cafe 
        => NetCafe.Value;

    internal static AssetManager Assets;
    internal static CustomerManager Customers;
    internal static MenuManager Menu;

    internal static Texture2D Sprites;

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
        helper.Events.Content.AssetRequested += AssetManager.OnAssetRequested;
        helper.Events.Content.AssetReady += AssetManager.OnAssetReady;

        Sprites = helper.ModContent.Load<Texture2D>("assets/sprites.png");
    }

   
    private void OnGameLaunched(object sender, GameLaunchedEventArgs e)
    {
        ISpaceCoreApi spacecore = ModHelper.ModRegistry.GetApi<ISpaceCoreApi>("spacechase0.SpaceCore");
        if (spacecore == null)
        {
            Log.Error("SpaceCore not found.");
            return;
        }

        spacecore.RegisterSerializerType(typeof(Cafe));
        spacecore.RegisterCustomProperty(typeof(Farm), "Cafe", typeof(NetRef<Cafe>), 
            AccessTools.Method(typeof(CafeSyncExtensions), nameof(CafeSyncExtensions.get_Cafe)), 
            AccessTools.Method(typeof(CafeSyncExtensions), nameof(CafeSyncExtensions.set_Cafe)));

        ModConfig.Initialize();

        ModHelper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
        ModHelper.Events.GameLoop.ReturnedToTitle += OnReturnedToTitle;
        ModHelper.Events.Multiplayer.ModMessageReceived += Sync.OnModMessageReceived;

        ModHelper.Events.Input.ButtonPressed += Debug.ButtonPress;

        GameLocation.RegisterTileAction(ModKeys.SIGNBOARD_BUILDING_CLICK_EVENT_KEY, delegate (GameLocation _, string[] _, Farmer _, Point _)
        {
            Game1.activeClickableMenu = null; // TODO do the thing
            return true;
        });
    }

    private void OnSaveLoaded(object sender, SaveLoadedEventArgs e)
    {
        ModHelper.Events.GameLoop.DayStarted += Cafe.DayUpdate;
        ModHelper.Events.GameLoop.TimeChanged += Cafe.OnTimeChanged;
        ModHelper.Events.Display.RenderedWorld += Cafe.OnRenderedWorld;

        Cafe.Initialize(ModHelper);

        if (!Context.IsMainPlayer)
            return;

        ModHelper.Events.Multiplayer.PeerConnected += Sync.OnPeerConnected;
    }

    private void OnReturnedToTitle(object sender, ReturnedToTitleEventArgs e)
    {
        ModHelper.Events.GameLoop.DayStarted -= Cafe.DayUpdate;
        ModHelper.Events.GameLoop.TimeChanged -= Cafe.OnTimeChanged;
        ModHelper.Events.World.FurnitureListChanged -= Cafe.OnFurnitureListChanged;
        ModHelper.Events.Display.RenderedWorld -= Cafe.OnRenderedWorld;
        ModHelper.Events.Multiplayer.PeerConnected -= Sync.OnPeerConnected;

        //if (CafeManager.Instance == null)
        //    return;
    }
}
