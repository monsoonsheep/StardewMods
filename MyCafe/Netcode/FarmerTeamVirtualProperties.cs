using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using MyCafe.Interfaces;
using Netcode;
using StardewValley;
using StardewValley.Network;

namespace MyCafe.Netcode;
public static class FarmerTeamVirtualProperties
{
    public class Holder { public NetRef<CafeNetObject> HeldValue = new(new CafeNetObject()); }
    public static ConditionalWeakTable<FarmerTeam, Holder> Values = [];

    internal static void Register(ISpaceCoreApi spaceCore)
    {
        // Again, DO WE NEED TO DO THIS????
        spaceCore.RegisterCustomProperty(
            typeof(FarmerTeam),
            "CafeNetFields",
            typeof(NetRef<CafeNetObject>),
            AccessTools.Method(typeof(FarmerTeamVirtualProperties), nameof(get_CafeNetFields)),
            AccessTools.Method(typeof(FarmerTeamVirtualProperties), nameof(set_CafeNetFields)));

        spaceCore.RegisterSerializerType(typeof(CafeNetObject));
    }

    public static NetRef<CafeNetObject> get_CafeNetFields(this FarmerTeam farm)
    {
        Holder holder = Values.GetOrCreateValue(farm);
        return holder.HeldValue;
    }

    public static void set_CafeNetFields(this FarmerTeam farm, NetRef<CafeNetObject> value)
    {
        //Log.Error("Setting Cafe field for Farm. Should this be happening?");
        //Holder holder = Values.GetOrCreateValue(team);
        //holder.Value = value;
    }
}
