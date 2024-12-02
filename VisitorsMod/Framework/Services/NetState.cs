using HarmonyLib;
using Netcode;
using StardewMods.VisitorsMod.Framework.Data;
using StardewMods.VisitorsMod.Framework.Game;
using StardewValley.Network;

namespace StardewMods.VisitorsMod.Framework.Services;
internal class NetState : Service
{
    private static NetState Instance = null!;

    private NetStateObject fields
        => Game1.player.team.get_VisitorNetState().Value;

    // Fields for syncing data
    internal NetStringDictionary<GeneratedSpriteData, NetRef<GeneratedSpriteData>> GeneratedSprites
        => this.fields.GeneratedSprites;

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
        __instance.NetFields.AddField(__instance.get_VisitorNetState());
        Instance.Log.Info("Adding netfields to FarmerTeam");
    }
}
