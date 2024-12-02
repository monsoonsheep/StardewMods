using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace StardewMods.VisitorsMod.Framework.Data;
public static class NpcState
{
    internal class Holder
    {
        
    }

    internal static ConditionalWeakTable<NPC, Holder> values = new();

    
}
