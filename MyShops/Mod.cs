global using System;
global using System.Collections.Generic;
global using System.Linq;
global using System.Text;
global using System.Threading.Tasks;
global using HarmonyLib;
global using Microsoft.Xna.Framework;
global using StardewMods.MyShops.Framework;
global using StardewModdingAPI;
global using StardewModdingAPI.Events;
global using StardewValley;
global using StardewMods.Common;

using StardewMods.MyShops.Framework.Services;
using StardewMods.MyShops.Framework.Data.Models;
using Microsoft.Xna.Framework.Graphics;
using StardewMods.MyShops.Framework.Data;
using StardewValley.TokenizableStrings;
using StardewMods.MyShops.Characters;
using StardewMods.MyShops.Framework.Game;

namespace StardewMods.MyShops;
public class Mod : StardewModdingAPI.Mod
{
    internal static Mod Instance = null!;

    private Texture2D sprites = null!;
    private Harmony harmony = null!;

    internal static Harmony Harmony => Instance.harmony;
    internal static IModHelper ModHelper => Instance.Helper;
    internal static IMultiplayerHelper Multiplayer => Instance.Helper.Multiplayer;
    internal static IManifest Manifest => Instance.ModManifest;
    internal static IReflectionHelper Reflection => Instance.Helper.Reflection;
    internal static IModEvents Events => Instance.Helper.Events;
    internal static ConfigModel Config { get; set; } = new ConfigModel();
    internal static Texture2D Sprites => Instance.sprites;
    internal static NetState NetState => NetState.Instance;
    internal static MultiplayerManager ModMultiplayer => MultiplayerManager.Instance;
    internal static SaveDataManager SaveData => SaveDataManager.Instance;
    internal static CafeManager Cafe => CafeManager.Instance;
    internal static TableManager Tables => TableManager.Instance;
    internal static CustomerManager Customers => CustomerManager.Instance;
    internal static LocationManager Locations => LocationManager.Instance;
    internal static EventManager CustomEvents => EventManager.Instance;
    internal static DialogueManager Dialogue => DialogueManager.Instance;

    internal Dictionary<string, CustomerData> CustomerData = [];
    internal Dictionary<string, VillagerCustomerModel> VillagerCustomerModels = [];
    internal Dictionary<string, VillagerCustomerData> VillagerData = [];

    public Mod()
        => Instance = this;

    public override void Entry(IModHelper helper)
    {
        Log.Monitor = base.Monitor;
        I18n.Init(this.Helper.Translation);
        this.harmony = new Harmony(base.ModManifest.UniqueID);
        this.Helper.Events.GameLoop.GameLaunched += this.OnGameLaunched;
        this.Helper.Events.GameLoop.SaveLoaded += this.OnSaveLoaded;
    }

    private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
    {
        this.sprites = Game1.content.Load<Texture2D>(Values.MODASSET_SPRITES);

        TokenParser.RegisterParser(
          Values.TOKEN_RANDOM_MENU_ITEM,
          (string[] query, out string replacement, Random random, Farmer player) =>
          {
              replacement = Mod.Cafe.Menu.ItemDictionary.Values.SelectMany(i => i).ToList().PickRandom()?.DisplayName ?? "Special";
              return true;
          });

        GameStateQuery.Register(
           Values.GAMESTATEQUERY_ISINDOORCAFE,
           (query, context) => !Game1.currentLocation.IsOutdoors);

        this.Helper.ConsoleCommands.Add("cafe_givesignboard", "Gives you the cafe signboard (if you want to skip the Gus 7-heart event", Locations.GiveSignboard);

        FarmerTeamVirtualProperties.InjectFields();
        NpcVirtualProperties.InjectFields();

        new SaveDataManager();
        SaveDataManager.Instance.Initialize();

        new NetState();
        NetState.Instance.Initialize();

        new AssetManager();
        AssetManager.Instance.Initialize();

        new MultiplayerManager();
        MultiplayerManager.Instance.Initialize();

        new TableManager();
        TableManager.Instance.Initialize();

        new LocationManager();
        LocationManager.Instance.Initialize();

        new CustomerManager();
        CustomerManager.Instance.Initialize();

        new CafeManager();
        CafeManager.Instance.Initialize();

        new EventManager();
        EventManager.Instance.Initialize();

        new DialogueManager();
        DialogueManager.Instance.Initialize();
    }

    private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
    {
        foreach (var model in this.VillagerCustomerModels)
            if (!this.VillagerData.ContainsKey(model.Key))
                this.VillagerData[model.Key] = new VillagerCustomerData(model.Key);

        Pathfinding.AddRoutesToFarm();

        Customers.CleanUpCustomers();
    }
}
