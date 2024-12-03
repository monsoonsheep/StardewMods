using System.Runtime.CompilerServices;

namespace StardewMods.ExtraNpcBehaviors.Framework.Data;
public static class NpcState
{
    internal class Holder
    {
        public int behaviorTimerAccumulation;
        public int behaviorTimeTotal;

        public bool isLookingAround = false;
        public int[] lookDirections = [];

        public bool isSitting = false;
        public Vector2 sittingOriginalPosition;
        public Vector2 lerpStartPosition;
        public Vector2 lerpEndPosition;
        public float lerpPosition = -1f;
        public float lerpDuration = -1f;
        public Action<NPC>? afterLerp;

        public int[] sittingSprites = [];
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

    public static bool get_isSitting(this NPC npc)
    {
        Holder holder = values.GetOrCreateValue(npc);
        return holder.isSitting;
    }

    public static void set_isSitting(this NPC npc, bool value)
    {
        Holder holder = values.GetOrCreateValue(npc);
        holder.isSitting = value;
    }
    public static Vector2 get_sittingOriginalPosition(this NPC npc)
    {
        Holder holder = values.GetOrCreateValue(npc);
        return holder.sittingOriginalPosition;
    }

    public static void set_sittingOriginalPosition(this NPC npc, Vector2 value)
    {
        Holder holder = values.GetOrCreateValue(npc);
        holder.sittingOriginalPosition = value;
    }

    public static Vector2 get_lerpStartPosition(this NPC npc)
    {
        Holder holder = values.GetOrCreateValue(npc);
        return holder.lerpStartPosition;
    }

    public static void set_lerpStartPosition(this NPC npc, Vector2 value)
    {
        Holder holder = values.GetOrCreateValue(npc);
        holder.lerpStartPosition = value;
    }

    public static Vector2 get_lerpEndPosition(this NPC npc)
    {
        Holder holder = values.GetOrCreateValue(npc);
        return holder.lerpEndPosition;
    }

    public static void set_lerpEndPosition(this NPC npc, Vector2 value)
    {
        Holder holder = values.GetOrCreateValue(npc);
        holder.lerpEndPosition = value;
    }

    public static float get_lerpPosition(this NPC npc)
    {
        Holder holder = values.GetOrCreateValue(npc);
        return holder.lerpPosition;
    }

    public static void set_lerpPosition(this NPC npc, float value)
    {
        Holder holder = values.GetOrCreateValue(npc);
        holder.lerpPosition = value;
    }

    public static float get_lerpDuration(this NPC npc)
    {
        Holder holder = values.GetOrCreateValue(npc);
        return holder.lerpDuration;
    }

    public static void set_lerpDuration(this NPC npc, float value)
    {
        Holder holder = values.GetOrCreateValue(npc);
        holder.lerpDuration = value;
    }

    public static Action<NPC>? get_afterLerp(this NPC npc)
    {
        Holder holder = values.GetOrCreateValue(npc);
        return holder.afterLerp;
    }

    public static void set_afterLerp(this NPC npc, Action<NPC>? value)
    {
        Holder holder = values.GetOrCreateValue(npc);
        holder.afterLerp = value;
    }

    public static int[] get_sittingSprites(this NPC npc)
    {
        Holder holder = values.GetOrCreateValue(npc);
        return holder.sittingSprites;
    }

    public static void set_sittingSprites(this NPC npc, int[] value)
    {
        Holder holder = values.GetOrCreateValue(npc);
        holder.sittingSprites = value;
    }
}
