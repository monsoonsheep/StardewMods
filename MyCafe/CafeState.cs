using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using MyCafe.Interfaces;
using MyCafe.Netcode;
using Netcode;
using StardewValley;
using StardewValley.Network;

namespace MyCafe;
public static class CafeState
{
    public class Holder { public NetRef<CafeNetFields> Value = new(new CafeNetFields()); }
    public static ConditionalWeakTable<FarmerTeam, Holder> Values = [];

    internal static void Register(ISpaceCoreApi spaceCore)
    {
        // Again, DO WE NEED TO DO THIS????
        spaceCore.RegisterCustomProperty(
            typeof(FarmerTeam),
            "CafeNetFields",
            typeof(NetRef<CafeNetFields>),
            AccessTools.Method(typeof(CafeState), nameof(get_CafeNetFields)),
            AccessTools.Method(typeof(CafeState), nameof(set_CafeNetFields)));

        spaceCore.RegisterSerializerType(typeof(CafeNetFields));
    }

    public static NetRef<CafeNetFields> get_CafeNetFields(this FarmerTeam farm)
    {
        Holder holder = Values.GetOrCreateValue(farm);
        return holder.Value;
    }

    public static void set_CafeNetFields(this FarmerTeam farm, NetRef<CafeNetFields> value)
    {
        //Log.Error("Setting Cafe field for Farm. Should this be happening?");
        //Holder holder = Values.GetOrCreateValue(team);
        //holder.Value = value;
    }
}
