using System.Runtime.CompilerServices;
using HarmonyLib;
using Netcode;
using StardewMods.MyShops.Framework.Netcode;

namespace StardewMods.MyShops.Framework.Game;

public static class FarmerTeamVirtualProperties
{
    public class Holder { public NetRef<NetStateObject> HeldValue = new(new NetStateObject()); }
    public static ConditionalWeakTable<FarmerTeam, Holder> Values = [];

    internal static void Register(SpaceCore.IApi spaceCore)
    {
        // Do we need to do this?
        spaceCore.RegisterCustomProperty(
            typeof(FarmerTeam),
            "NetStateObject",
            typeof(NetRef<NetStateObject>),
            AccessTools.Method(typeof(FarmerTeamVirtualProperties), nameof(get_NetStateObject)),
            AccessTools.Method(typeof(FarmerTeamVirtualProperties), nameof(set_NetStateObject)));

        // spaceCore.RegisterSerializerType(typeof(NetState));
    }

    public static NetRef<NetStateObject> get_NetStateObject(this FarmerTeam farmerTeam)
    {
        Holder holder = Values.GetOrCreateValue(farmerTeam);
        return holder.HeldValue;
    }

    public static void set_NetStateObject(this FarmerTeam farmerTeam, NetRef<NetStateObject> value)
    {
        //Log.Error("Setting field for FarmerTeam. Should this be happening?");
        //Holder holder = Values.GetOrCreateValue(team);
        //holder.Value = value;
    }
}
