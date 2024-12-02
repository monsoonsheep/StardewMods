using System.Runtime.CompilerServices;
using HarmonyLib;
using Netcode;

namespace StardewMods.VisitorsMod.Framework.Game;

public static class FarmerTeamVirtualProperties
{
    public class Holder { public NetRef<NetStateObject> HeldValue = new(new NetStateObject()); }
    public static ConditionalWeakTable<FarmerTeam, Holder> Values = [];

    internal static void Register(ISpaceCoreApi spaceCore)
    {
        // Do we need to do this?
        spaceCore.RegisterCustomProperty(
            typeof(FarmerTeam),
            "VisitorNetState",
            typeof(NetRef<NetStateObject>),
            AccessTools.Method(typeof(FarmerTeamVirtualProperties), nameof(get_VisitorNetState)),
            AccessTools.Method(typeof(FarmerTeamVirtualProperties), nameof(set_VisitorNetState)));

        // spaceCore.RegisterSerializerType(typeof(NetState));
    }

    public static NetRef<NetStateObject> get_VisitorNetState(this FarmerTeam farmerTeam)
    {
        Holder holder = Values.GetOrCreateValue(farmerTeam);
        return holder.HeldValue;
    }

    public static void set_VisitorNetState(this FarmerTeam farmerTeam, NetRef<NetStateObject> value)
    {
        //Log.Error("Setting field for FarmerTeam. Should this be happening?");
        //Holder holder = Values.GetOrCreateValue(team);
        //holder.Value = value;
    }
}
