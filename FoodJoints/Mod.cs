global using System;
global using System.Collections.Generic;
global using System.Linq;
global using System.Text;
global using System.Threading.Tasks;
global using HarmonyLib;
global using Microsoft.Xna.Framework;
global using StardewMods.FoodJoints.Framework;
global using StardewModdingAPI;
global using StardewModdingAPI.Events;
global using StardewValley;
global using StardewMods.Common;

using StardewMods.FoodJoints.Framework.Services;
using StardewMods.FoodJoints.Framework.Data.Models;
using Microsoft.Xna.Framework.Graphics;
using StardewValley.TokenizableStrings;
using StardewMods.FoodJoints.Framework.Game;
using StardewMods.SheepCore.Framework.Services;

namespace StardewMods.FoodJoints;
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
    internal static AssetManager Assets => AssetManager.Instance;
    internal static SaveDataManager SaveData => SaveDataManager.Instance;
    internal static CafeManager Cafe => CafeManager.Instance;
    internal static TableManager Tables => TableManager.Instance;
    internal static CustomerManager Customers => CustomerManager.Instance;
    internal static LocationManager Locations => LocationManager.Instance;
    internal static EventManager CustomEvents => EventManager.Instance;
    internal static DialogueManager Dialogue => DialogueManager.Instance;
    internal static Pathfinding Pathfinding => Pathfinding.Instance;

    public Mod()
        => Instance = this;

    public override void Entry(IModHelper helper)
    {
        Log.Monitor = base.Monitor;
        I18n.Init(this.Helper.Translation);
        this.harmony = new Harmony(base.ModManifest.UniqueID);
        this.Helper.Events.GameLoop.GameLaunched += this.OnGameLaunched;
    }

    private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
    {
        FarmerTeamVirtualProperties.InjectFields();
        NpcVirtualProperties.InjectFields();

        _ = new ConfigManager();

        _ = new SaveDataManager();

        _ = new NetState();

        _ = new AssetManager();

        _ = new MultiplayerManager();

        _ = new TableManager();

        _ = new LocationManager();

        _ = new CustomerManager();

        _ = new CafeManager();

        _ = new EventManager();

        _ = new DialogueManager();

        _ = new ActionPatches();

        _ = new CharacterPatches();

        _ = new Debug();

        this.Helper.ConsoleCommands.Add("cafe_givesignboard", "Gives you the cafe signboard (if you want to skip the Gus 7-heart event", Locations.GiveSignboard);

        TokenParser.RegisterParser(
          Values.TOKEN_RANDOM_MENU_ITEM,
          (string[] query, out string replacement, Random random, Farmer player) =>
          {
              replacement = Cafe.Menu.ItemDictionary.Values.SelectMany(i => i).ToList().PickRandom()?.DisplayName ?? "Special";
              return true;
          });

        GameStateQuery.Register(
           Values.GAMESTATEQUERY_ISINDOORCAFE,
           (query, context) => !Game1.currentLocation.IsOutdoors);

        this.sprites = Game1.content.Load<Texture2D>(Values.MODASSET_SPRITES);
    }
}
