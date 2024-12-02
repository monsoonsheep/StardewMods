using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewMods.ExtraNpcBehaviors.Framework.Data;
using StardewValley.Pathfinding;

namespace StardewMods.ExtraNpcBehaviors.Framework.Services.Visitors;
internal class EndBehaviors : Service
{
    private static EndBehaviors instance = null!;

    public EndBehaviors(
        Harmony harmony,
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
           original: AccessTools.Method(typeof(NPC), nameof(NPC.updateMovement), [typeof(GameLocation), typeof(GameTime)]),
           postfix: new HarmonyMethod(AccessTools.Method(this.GetType(), nameof(After_updateMovement)))
        );
    }

    private static void After_getRouteEndBehaviorFunction(NPC __instance, string behaviorName, string endMessage, ref PathFindController.endBehavior __result)
    {
        if (__result == null)
        {
            if (behaviorName != null && behaviorName.StartsWith("look_"))
            {
                __instance.endOfRouteBehaviorName.Value = behaviorName;
                __result = instance.startLookAroundBehavior;
            }
        }
    }

    private static void After_updateMovement(NPC __instance, GameLocation location, GameTime time)
    {
        if (NpcState.values.TryGetValue(__instance, out var holder))
        {
            if (holder.isLookingAround)
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
                        (direction == dominantDirection) ? 6000 : 2000,
                        (direction == dominantDirection) ? 12000 : 7000);
                }
            }
        }
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

            npc.set_lookDirections(directions);
            npc.set_isLookingAround(true);
        }
    }
}
