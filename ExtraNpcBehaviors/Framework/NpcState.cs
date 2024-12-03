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

    internal static ConditionalWeakTable<NPC, Holder> table = new();

    public static bool get_isLookingAround(this NPC npc)
    {
        Holder values = table.GetOrCreateValue(npc);
        return values.isLookingAround;
    }

    public static void set_isLookingAround(this NPC npc, bool value)
    {
        Holder values = table.GetOrCreateValue(npc);
        values.isLookingAround = value;
    }

    public static int get_behaviorTimerAccumulation(this NPC npc)
    {
        Holder values = table.GetOrCreateValue(npc);
        return values.behaviorTimerAccumulation;
    }

    public static void set_behaviorTimerAccumulation(this NPC npc, int value)
    {
        Holder values = table.GetOrCreateValue(npc);
        values.behaviorTimerAccumulation = value;
    }

    public static int get_behaviorTimeTotal(this NPC npc)
    {
        Holder values = table.GetOrCreateValue(npc);
        return values.behaviorTimeTotal;
    }

    public static void set_behaviorTimeTotal(this NPC npc, int value)
    {
        Holder values = table.GetOrCreateValue(npc);
        values.behaviorTimeTotal = value;
    }

    public static int[] get_lookDirections(this NPC npc)
    {
        Holder values = table.GetOrCreateValue(npc);
        return values.lookDirections;
    }

    public static void set_lookDirections(this NPC npc, int[] value)
    {
        Holder values = table.GetOrCreateValue(npc);
        values.lookDirections = value;
    }

    public static bool get_isSitting(this NPC npc)
    {
        Holder values = table.GetOrCreateValue(npc);
        return values.isSitting;
    }

    public static void set_isSitting(this NPC npc, bool value)
    {
        Holder values = table.GetOrCreateValue(npc);
        values.isSitting = value;
    }
    public static Vector2 get_sittingOriginalPosition(this NPC npc)
    {
        Holder values = table.GetOrCreateValue(npc);
        return values.sittingOriginalPosition;
    }

    public static void set_sittingOriginalPosition(this NPC npc, Vector2 value)
    {
        Holder values = table.GetOrCreateValue(npc);
        values.sittingOriginalPosition = value;
    }

    public static Vector2 get_lerpStartPosition(this NPC npc)
    {
        Holder values = table.GetOrCreateValue(npc);
        return values.lerpStartPosition;
    }

    public static void set_lerpStartPosition(this NPC npc, Vector2 value)
    {
        Holder values = table.GetOrCreateValue(npc);
        values.lerpStartPosition = value;
    }

    public static Vector2 get_lerpEndPosition(this NPC npc)
    {
        Holder values = table.GetOrCreateValue(npc);
        return values.lerpEndPosition;
    }

    public static void set_lerpEndPosition(this NPC npc, Vector2 value)
    {
        Holder values = table.GetOrCreateValue(npc);
        values.lerpEndPosition = value;
    }

    public static float get_lerpPosition(this NPC npc)
    {
        Holder values = table.GetOrCreateValue(npc);
        return values.lerpPosition;
    }

    public static void set_lerpPosition(this NPC npc, float value)
    {
        Holder values = table.GetOrCreateValue(npc);
        values.lerpPosition = value;
    }

    public static float get_lerpDuration(this NPC npc)
    {
        Holder values = table.GetOrCreateValue(npc);
        return values.lerpDuration;
    }

    public static void set_lerpDuration(this NPC npc, float value)
    {
        Holder values = table.GetOrCreateValue(npc);
        values.lerpDuration = value;
    }

    public static Action<NPC>? get_afterLerp(this NPC npc)
    {
        Holder values = table.GetOrCreateValue(npc);
        return values.afterLerp;
    }

    public static void set_afterLerp(this NPC npc, Action<NPC>? value)
    {
        Holder values = table.GetOrCreateValue(npc);
        values.afterLerp = value;
    }

    public static int[] get_sittingSprites(this NPC npc)
    {
        Holder values = table.GetOrCreateValue(npc);
        return values.sittingSprites;
    }

    public static void set_sittingSprites(this NPC npc, int[] value)
    {
        Holder values = table.GetOrCreateValue(npc);
        values.sittingSprites = value;
    }
}
