using StardewMods.ExtraNpcBehaviors.Framework.Data;
using StardewValley.GameData.Characters;
using StardewValley.Pathfinding;

namespace StardewMods.ExtraNpcBehaviors.Framework.Services.Visitors;
internal class EndBehaviors : Service
{
    private static EndBehaviors instance = null!;

    public EndBehaviors(
        Harmony harmony,
        IModEvents events,
        ILogger logger,
        IManifest manifest
        ) : base(logger, manifest)
    {
        instance = this;

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

    private static void After_update(NPC __instance, GameTime time, GameLocation location)
    {
        if (NpcState.values.TryGetValue(__instance, out var holder))
        {
            // Lerping the position of the character (for sitting and getting up)
            if (holder.lerpPosition >= 0f)
            {
                holder.lerpPosition = holder.lerpPosition + (float)time.ElapsedGameTime.TotalSeconds;

                if (holder.lerpPosition >= holder.lerpDuration)
                {
                    holder.lerpPosition = holder.lerpDuration;
                }

                __instance.Position = new Vector2(
                    Utility.Lerp(holder.lerpStartPosition.X, holder.lerpEndPosition.X, holder.lerpPosition / holder.lerpDuration),
                    Utility.Lerp(holder.lerpStartPosition.Y, holder.lerpEndPosition.Y, holder.lerpPosition / holder.lerpDuration));

                if (holder.lerpPosition >= holder.lerpDuration)
                {
                    holder.afterLerp?.Invoke(__instance);
                    holder.afterLerp = null;
                    holder.lerpPosition = -1f;
                }
            }
            else if (holder.isLookingAround)
            {
                holder.behaviorTimerAccumulation += time.ElapsedGameTime.Milliseconds;

                if (holder.behaviorTimerAccumulation >= holder.behaviorTimeTotal)
                {
                    int dominantDirection = -1;

                    if (holder.lookDirections.Count(i => i == 0) > 1)
                        dominantDirection = 0;
                    if (holder.lookDirections.Count(i => i == 1) > 1)
                        dominantDirection = 1;
                    if (holder.lookDirections.Count(i => i == 2) > 1)
                        dominantDirection = 2;
                    if (holder.lookDirections.Count(i => i == 3) > 1)
                        dominantDirection = 3;

                    int direction = holder.lookDirections.OrderBy(_ => Game1.random.Next()).First(i => i != dominantDirection);

                    if (Game1.random.NextDouble() < 0.5f)
                    {
                        direction = dominantDirection;
                    }

                    __instance.faceDirection(direction);
                    holder.behaviorTimerAccumulation = 0;
                    holder.behaviorTimeTotal = Game1.random.Next(
                        (direction == dominantDirection) ? 6000 : 1000,
                        (direction == dominantDirection) ? 12000 : 5000);
                }
            }
        }
    }

    private static bool Before_finishEndOfRouteAnimation(NPC __instance)
    {
        if (NpcState.values.TryGetValue(__instance, out var holder))
        {
            if (holder.isSitting)
            {
                // Get up from chair
                holder.isSitting = false;
                holder.lerpStartPosition = __instance.Position;
                holder.lerpEndPosition = holder.sittingOriginalPosition;
                holder.lerpPosition = 0f;
                holder.lerpDuration = 0.3f;
                holder.afterLerp = delegate (NPC n)
                {
                    AccessTools.Method(typeof(NPC), "routeEndAnimationFinished", [typeof(Farmer)]).Invoke(n, [null]);
                };

                __instance.Sprite.ClearAnimation();

                return false;
            }
        }

        return true;
    }

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

    public void startSitBehavior(Character c, GameLocation loc)
    {
        if (c is NPC npc)
        {
            string behaviorName = npc.endOfRouteBehaviorName.Value;
            string[] split = behaviorName.Split('_');

            int jumpDirection = int.Parse(split[1]);

            Vector2 seatPosition = npc.Position + (jumpDirection) switch
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
