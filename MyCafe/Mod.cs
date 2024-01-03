using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using MyCafe.Framework;
using MyCafe.Framework.Managers;
using Microsoft.Xna.Framework.Graphics;

namespace MyCafe;

internal class Mod : StardewModdingAPI.Mod
{
    internal new static IMonitor Monitor;
    internal static IModHelper ModHelper;
    internal new static IManifest ModManifest;

    internal static CafeManager cafe;
    internal static AssetManager assets;
    internal static CustomerManager customers;
    internal static TableManager tables;
    internal static MenuManager menu;

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

        AssetManager.Sprites = helper.ModContent.Load<Texture2D>("assets/sprites.png");
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
        
        cafe = new CafeManager();
        assets = new AssetManager();
        customers = new CustomerManager();
        tables = new TableManager();
        menu = new MenuManager();

        assets.LoadValuesFromModData();
        cafe.UpdateCafeIndoorLocation();
        cafe.PopulateRoutesToCafe();
        tables.PopulateTables(Game1.getFarm(), cafe.CafeIndoors);
        customers.PopulateCustomersData();

        ModHelper.Events.GameLoop.DayStarted += cafe.DayUpdate;
        ModHelper.Events.GameLoop.TimeChanged += cafe.OnTimeChanged;
        ModHelper.Events.World.FurnitureListChanged += tables.OnFurnitureListChanged;
        ModHelper.Events.Display.RenderedWorld += tables.OnRenderedWorld;
        ModHelper.Events.Multiplayer.PeerConnected += Sync.OnPeerConnected;
    }

    private void OnReturnedToTitle(object sender, ReturnedToTitleEventArgs e)
    {
        if (CafeManager.Instance == null)
            return;

        ModHelper.Events.GameLoop.DayStarted -= cafe.DayUpdate;
        ModHelper.Events.GameLoop.TimeChanged -= cafe.OnTimeChanged;
        ModHelper.Events.World.FurnitureListChanged -= tables.OnFurnitureListChanged;
        ModHelper.Events.Display.RenderedWorld -= tables.OnRenderedWorld;
        ModHelper.Events.Multiplayer.PeerConnected -= Sync.OnPeerConnected;
    }
}