using HarmonyLib;
using StardewMods.ExtraNpcBehaviors.Framework.Data;
using StardewValley;
using StardewValley.GameData.Characters;
using StardewValley.Pathfinding;

namespace StardewMods.ExtraNpcBehaviors.Framework;
internal class EndBehaviors
{
    private static EndBehaviors instance = null!;

    public EndBehaviors()
        => instance = this;

    internal void Initialize()
    {
        Harmony harmony = Mod.Harmony;

        harmony.Patch(
            original: AccessTools.Method(typeof(NPC), "getRouteEndBehaviorFunction", [typeof(string), typeof(string)]),
            postfix: new HarmonyMethod(AccessTools.Method(this.GetType(), nameof(After_getRouteEndBehaviorFunction)))
        );
        harmony.Patch(
            original: AccessTools.Method(typeof(NPC), nameof(NPC.update), [typeof(GameTime), typeof(GameLocation)]),
            postfix: new HarmonyMethod(AccessTools.Method(this.GetType(), nameof(After_update)))
        );
        harmony.Patch(
            original: AccessTools.Method(typeof(NPC), "finishEndOfRouteAnimation", []),
            prefix: new HarmonyMethod(AccessTools.Method(this.GetType(), nameof(Before_finishEndOfRouteAnimation)))
        );
    }

    /// <summary>
    /// Return a <see cref="PathFindController.endBehavior"/> method if the requested behavior name matches a pattern for a modded behavior
    /// </summary>
    private static void After_getRouteEndBehaviorFunction(NPC __instance, string behaviorName, string endMessage, ref PathFindController.endBehavior __result)
    {
        if (__result == null && behaviorName != null)
        {
            if (behaviorName.StartsWith("look_"))
            {
                __instance.endOfRouteBehaviorName.Value = behaviorName;
                __result = instance.startLookAroundBehavior;
            }
            else if (behaviorName.StartsWith("sit_"))
            {
                __instance.endOfRouteBehaviorName.Value = behaviorName;
                __result = instance.startSitBehavior;
            }
        }
    }

    /// <summary>
    /// Update states every tick for modded behaviors
    /// </summary>
    private static void After_update(NPC __instance, GameTime time, GameLocation location)
    {
        __instance.speed = 4;
        if (NpcVirtualProperties.Table.TryGetValue(__instance, out var values))
        {
            // Lerping the position of the character (for sitting and getting up)
            if (values.lerpPosition >= 0f)
            {
                values.lerpPosition = values.lerpPosition + (float) time.ElapsedGameTime.TotalSeconds;

                if (values.lerpPosition >= values.lerpDuration)
                {
                    values.lerpPosition = values.lerpDuration;
                }

                __instance.Position = new Vector2(
                    Utility.Lerp(values.lerpStartPosition.X, values.lerpEndPosition.X, values.lerpPosition / values.lerpDuration),
                    Utility.Lerp(values.lerpStartPosition.Y, values.lerpEndPosition.Y, values.lerpPosition / values.lerpDuration));

                if (values.lerpPosition >= values.lerpDuration)
                {
                    values.afterLerp?.Invoke(__instance);
                    values.afterLerp = null;
                    values.lerpPosition = -1f;
                }
            }
            else if (values.isLookingAround)
            {
                values.behaviorTimerAccumulation += time.ElapsedGameTime.Milliseconds;

                if (values.behaviorTimerAccumulation >= values.behaviorTimeTotal)
                {
                    int dominantDirection = -1;

                    if (values.lookDirections.Count(i => i == 0) > 1)
                        dominantDirection = 0;
                    if (values.lookDirections.Count(i => i == 1) > 1)
                        dominantDirection = 1;
                    if (values.lookDirections.Count(i => i == 2) > 1)
                        dominantDirection = 2;
                    if (values.lookDirections.Count(i => i == 3) > 1)
                        dominantDirection = 3;

                    int direction = values.lookDirections.OrderBy(_ => Game1.random.Next()).First(i => i != dominantDirection);

                    if (Game1.random.NextDouble() < 0.5f)
                    {
                        direction = dominantDirection;
                    }

                    __instance.faceDirection(direction);
                    values.behaviorTimerAccumulation = 0;
                    values.behaviorTimeTotal = Game1.random.Next(
                        direction == dominantDirection ? 6000 : 1000,
                        direction == dominantDirection ? 12000 : 5000);
                }
            }
        }
    }

    /// <summary>
    /// Block finishEndOfRouteAnimation for an NPC at the end of a modded behavior
    /// </summary>
    private static bool Before_finishEndOfRouteAnimation(NPC __instance)
    {
        if (NpcVirtualProperties.Table.TryGetValue(__instance, out var values))
        {
            if (values.isSitting)
            {
                // Get up from chair
                values.isSitting = false;

                values.lerpStartPosition = __instance.Position;
                values.lerpEndPosition = values.sittingOriginalPosition;
                values.lerpPosition = 0f;
                values.lerpDuration = 0.3f;

                values.afterLerp = delegate (NPC n)
                {
                    NpcVirtualProperties.Table.Remove(n);
                    AccessTools.Field(typeof(NPC), "freezeMotion").SetValue(n, false);
                    AccessTools.Method(typeof(NPC), "routeEndAnimationFinished", [typeof(Farmer)]).Invoke(n, [null]);
                    n.Sprite.ClearAnimation();
                };


                return false;
            }

        }

        return true;
    }

    /// <summary>
    /// Start the "look_" behavior, set the custom states that'll be managed in the update tick patch
    /// This is called by the PathfindController, at the end of a route
    /// </summary>
    public void startLookAroundBehavior(Character c, GameLocation loc)
    {
        if (c is NPC npc)
        {
            string behaviorName = npc.endOfRouteBehaviorName.Value;
            string[] split = behaviorName.Split('_');
            int[] directions = new int[split.Length - 1];

            for (int i = 1; i < split.Length; i++)
            {
                int d = int.Parse(split[i]);
                directions[i - 1] = d;
            }

            npc.set_isLookingAround(true);
            npc.set_lookDirections(directions);

            AccessTools.Field(typeof(NPC), "freezeMotion").SetValue(npc, true);
        }
    }

    /// <summary>
    /// Start the "sit_" behavior, set the custom states that'll be managed in the update tick patch
    /// This is called by the PathfindController, at the end of a route
    /// </summary>
    public void startSitBehavior(Character c, GameLocation loc)
    {
        if (c is NPC npc)
        {
            string behaviorName = npc.endOfRouteBehaviorName.Value;
            string[] split = behaviorName.Split('_');

            int jumpDirection = int.Parse(split[1]);

            Vector2 seatPosition = npc.Position + jumpDirection switch
            {
                0 => new Vector2(0f, -64f),
                1 => new Vector2(64f, 0f),
                2 => new Vector2(0f, 64f),
                3 => new Vector2(-64f, 0f),
                _ => throw new NotImplementedException()
            };

            npc.set_lerpStartPosition(npc.Position);
            npc.set_lerpEndPosition(seatPosition);
            npc.set_lerpPosition(0f);
            npc.set_lerpDuration(0.3f);
            npc.set_isSitting(true);
            npc.set_sittingOriginalPosition(npc.Position);

            npc.faceDirection(int.Parse(split[2]));

            AccessTools.Field(typeof(NPC), "freezeMotion").SetValue(npc, true);

            // Set sitting sprite if it exists

            CharacterData? data = npc.GetData();
            if ((data?.CustomFields != null && data.CustomFields.TryGetValue(Values.DATA_SITTINGSPRITES, out string? val))
                || npc.modData.TryGetValue(Values.DATA_SITTINGSPRITES, out val))
            {
                string[] frames = val.Split(' ');

                if (frames.Length == 4)
                {
                    int[] sittingSprites = frames.Select(s => int.Parse(s)).ToArray();
                    npc.set_sittingSprites(sittingSprites);
                    int sittingFrame = sittingSprites[npc.FacingDirection];
                    npc.Sprite.setCurrentAnimation([new FarmerSprite.AnimationFrame(sittingFrame, int.MaxValue)]);
                }
            }
        }
    }
}
