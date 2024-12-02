using System.Runtime.CompilerServices;
using HarmonyLib;
using Monsoonsheep.StardewMods.MyCafe.Interfaces;
using Monsoonsheep.StardewMods.MyCafe.Netcode;
using Netcode;
using StardewValley;

namespace Monsoonsheep.StardewMods.MyCafe.Game;
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

        // spaceCore.RegisterSerializerType(typeof(CafeNetObject));
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
