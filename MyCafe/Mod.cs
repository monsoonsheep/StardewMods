using Microsoft.Xna.Framework.Graphics;
using MyCafe.Framework;
using MyCafe.Framework.Managers;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace MyCafe;

internal class Mod : StardewModdingAPI.Mod
{
    internal new static IMonitor Monitor;
    internal static IModHelper ModHelper;
    internal new static IManifest ModManifest;

    internal static CafeManager Cafe;
    internal static AssetManager Assets;
    internal static CustomerManager Customers;
    internal static TableManager Tables;
    internal static MenuManager Menu;
    internal static BusCustomerSpawner BusCustomers;
    internal static VillagerCustomerSpawner VillagerCustomers;

    internal static Texture2D Sprites;

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

        Sprites = helper.ModContent.Load<Texture2D>("assets/sprites.png");
    }

    private void OnGameLaunched(object sender, GameLaunchedEventArgs e)
    {
        Config.Initialize();

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

        Cafe = new CafeManager();
        Assets = new AssetManager();
        Customers = new CustomerManager();
        Tables = new TableManager();
        Menu = new MenuManager();
        BusCustomers = new BusCustomerSpawner();
        VillagerCustomers = new VillagerCustomerSpawner();


        Cafe.UpdateCafeIndoorLocation();
        Cafe.PopulateRoutesToCafe();
        Tables.PopulateTables(Game1.getFarm(), Cafe.CafeIndoors);

        Assets.LoadContentPacks(ModHelper);
        Assets.LoadStoredCustomerData();

        VillagerCustomers.LoadNpcSchedules();

        ModHelper.Events.GameLoop.DayStarted += Cafe.DayUpdate;
        ModHelper.Events.GameLoop.TimeChanged += Cafe.OnTimeChanged;
        ModHelper.Events.World.FurnitureListChanged += Tables.OnFurnitureListChanged;
        ModHelper.Events.Display.RenderedWorld += Tables.OnRenderedWorld;
        ModHelper.Events.Multiplayer.PeerConnected += Sync.OnPeerConnected;
    }

    private void OnReturnedToTitle(object sender, ReturnedToTitleEventArgs e)
    {
        if (CafeManager.Instance == null)
            return;

        ModHelper.Events.GameLoop.DayStarted -= Cafe.DayUpdate;
        ModHelper.Events.GameLoop.TimeChanged -= Cafe.OnTimeChanged;
        ModHelper.Events.World.FurnitureListChanged -= Tables.OnFurnitureListChanged;
        ModHelper.Events.Display.RenderedWorld -= Tables.OnRenderedWorld;
        ModHelper.Events.Multiplayer.PeerConnected -= Sync.OnPeerConnected;
    }
}