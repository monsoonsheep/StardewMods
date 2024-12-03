using System.Runtime.CompilerServices;

namespace StardewMods.VisitorsMod.Framework.Data;
public static class NpcState
{
    internal class Holder
    {
        
    }

    internal static ConditionalWeakTable<NPC, Holder> values = new();

    
}
