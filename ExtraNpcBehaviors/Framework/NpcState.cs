using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace StardewMods.ExtraNpcBehaviors.Framework.Data;
public static class NpcState
{
    internal class Holder
    {
        public int behaviorTimerAccumulation;
        public int behaviorTimeTotal;

        public bool isLookingAround = false;
        public int[] lookDirections = [];
    }

    internal static ConditionalWeakTable<NPC, Holder> values = new();

    public static bool get_isLookingAround(this NPC npc)
    {
        Holder holder = values.GetOrCreateValue(npc);
        return holder.isLookingAround;
    }

    public static void set_isLookingAround(this NPC npc, bool value)
    {
        Holder holder = values.GetOrCreateValue(npc);
        holder.isLookingAround = value;
    }

    public static int get_behaviorTimerAccumulation(this NPC npc)
    {
        Holder holder = values.GetOrCreateValue(npc);
        return holder.behaviorTimerAccumulation;
    }

    public static void set_behaviorTimerAccumulation(this NPC npc, int value)
    {
        Holder holder = values.GetOrCreateValue(npc);
        holder.behaviorTimerAccumulation = value;
    }

    public static int get_behaviorTimeTotal(this NPC npc)
    {
        Holder holder = values.GetOrCreateValue(npc);
        return holder.behaviorTimeTotal;
    }

    public static void set_behaviorTimeTotal(this NPC npc, int value)
    {
        Holder holder = values.GetOrCreateValue(npc);
        holder.behaviorTimeTotal = value;
    }

    public static int[] get_lookDirections(this NPC npc)
    {
        Holder holder = values.GetOrCreateValue(npc);
        return holder.lookDirections;
    }

    public static void set_lookDirections(this NPC npc, int[] value)
    {
        Holder holder = values.GetOrCreateValue(npc);
        holder.lookDirections = value;
    }
}
