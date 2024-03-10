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

namespace MyCafe;
public static class CafeState
{
    internal class Holder { public readonly NetRef<Cafe> Value = [new Cafe()]; }

    internal static ConditionalWeakTable<Farm, Holder> Values = [];

    internal static void Register(ISpaceCoreApi spaceCore)
    {
        spaceCore.RegisterCustomProperty(
            typeof(Farm),
            "Cafe",
            typeof(NetRef<Cafe>),
            AccessTools.Method(typeof(CafeState), nameof(get_Cafe)),
            AccessTools.Method(typeof(CafeState), nameof(set_Cafe)));
    }

    public static NetRef<Cafe> get_Cafe(this Farm farm)
    {
        Holder holder = Values.GetOrCreateValue(farm);
        return holder.Value;
    }

    public static void set_Cafe(this Farm farm, NetRef<Cafe> value)
    {
        //Log.Error("Setting Cafe field for Farm. Should this be happening?");
        //Holder holder = Values.GetOrCreateValue(farm);
        //holder.Value = value;
    }
}
