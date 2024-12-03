using HarmonyLib;
using Netcode;
using StardewMods.MyShops.Framework.Game;
using StardewMods.MyShops.Framework.Inventories;
using StardewMods.MyShops.Framework.Objects;
using StardewValley.Network;

namespace StardewMods.MyShops.Framework.Services;
internal class NetState : Service
{
    private static NetState Instance = null!;

    internal NetStateObject fields
        => Game1.player.team.get_NetStateObject().Value;

    internal NetCollection<Table> Tables
        => this.fields.Tables;

    internal NetBool CafeEnabled
        => this.fields.CafeEnabled;

    internal NetBool CafeOpen
        => this.fields.CafeOpen;

    internal NetRef<StardewValley.Object?> Signboard
        => this.fields.Signboard;

    internal NetRef<FoodMenuInventory> Menu
        => this.fields.Menu;

    // Fields for syncing data
    
    public NetState(
        Harmony harmony,
        ILogger logger,
        IManifest manifest)
        : base(logger, manifest)
    {
        Instance = this;

        harmony.Patch(
            original: AccessTools.Constructor(typeof(FarmerTeam)),
            postfix: new HarmonyMethod(AccessTools.Method(this.GetType(), nameof(After_FarmerTeamConstructor)))
        );
    }


    /// <summary>
    /// Add the net field to the farmerTeam
    /// </summary>
    private static void After_FarmerTeamConstructor(FarmerTeam __instance)
    {
        __instance.NetFields.AddField(__instance.get_NetStateObject());
        Instance.Log.Info("Adding netfields to FarmerTeam");
    }
}
