using System.Runtime.CompilerServices;
using HarmonyLib;
using Netcode;
using StardewMods.MyShops.Framework.Netcode;

namespace StardewMods.MyShops.Framework.Game;

public static class FarmerTeamVirtualProperties
{
    public class Holder {
        public NetRef<CafeNetState> CafeNetState = new(new CafeNetState());
    }

    public static ConditionalWeakTable<FarmerTeam, Holder> Values = [];

    internal static void Register(SpaceCore.IApi spaceCore)
    {
        // Do we need to do this?
        spaceCore.RegisterCustomProperty(
            typeof(FarmerTeam),
            "NetStateObject",
            typeof(NetRef<CafeNetState>),
            AccessTools.Method(typeof(FarmerTeamVirtualProperties), nameof(get_CafeNetState)),
            AccessTools.Method(typeof(FarmerTeamVirtualProperties), nameof(set_CafeNetState)));

        // spaceCore.RegisterSerializerType(typeof(NetState));
    }

    internal static void InjectFields()
    {
        Mod.Harmony.Patch(
            original: AccessTools.Constructor(typeof(FarmerTeam)),
            postfix: new HarmonyMethod(AccessTools.Method(typeof(FarmerTeamVirtualProperties), nameof(After_FarmerTeamConstructor)))
        );
    }

    /// <summary>
    /// Add net fields to FarmerTeam
    /// </summary>
    private static void After_FarmerTeamConstructor(FarmerTeam __instance)
    {
        __instance.NetFields.AddField(__instance.get_CafeNetState());
        Log.Trace("Adding netfields to FarmerTeam");
    }

    public static NetRef<CafeNetState> get_CafeNetState(this FarmerTeam farmerTeam)
    {
        Holder holder = Values.GetOrCreateValue(farmerTeam);
        return holder.CafeNetState;
    }

    public static void set_CafeNetState(this FarmerTeam farmerTeam, NetRef<CafeNetState> value)
    {
        //Log.Error("Setting field for FarmerTeam. Should this be happening?");
        //Holder holder = Values.GetOrCreateValue(team);
        //holder.Value = value;
    }
}
