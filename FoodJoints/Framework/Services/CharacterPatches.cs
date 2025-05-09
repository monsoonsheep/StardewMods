using System.Reflection.Emit;
using StardewMods.FoodJoints.Framework.Characters;
using StardewMods.FoodJoints.Framework.Game;

namespace StardewMods.FoodJoints.Framework.Services;
internal class CharacterPatches
{
    internal static CharacterPatches Instance = null!;
    internal CharacterPatches()
    {
        Instance = this;

        Mod.Harmony.Patch(
            original: AccessTools.Method(typeof(Character), nameof(Character.shouldCollideWithBuildingLayer), [typeof(GameLocation)]),
            transpiler: new HarmonyMethod(AccessTools.Method(this.GetType(), nameof(CharacterPatches.Transpile_CharacterShouldCollideWithBuildingLayer)))
        );
        Mod.Harmony.Patch(
            original: AccessTools.Method(typeof(NPC), nameof(NPC.update), [typeof(GameTime), typeof(GameLocation)]),
            postfix: new HarmonyMethod(AccessTools.Method(this.GetType(), nameof(CharacterPatches.After_NpcUpdate)))
        );
    }

    private static void After_NpcUpdate(NPC __instance, GameTime time, GameLocation location)
    {
        // To optimize, add npc names to a list when they are customers
        if (__instance.get_OrderItem().Value != null)
        {
            __instance.speed = 4;

            // If they are in Bus Stop or town and there's no farmers there, warp them to the next location in their route
            if (Mod.Config.WarpCustomers
                && __instance.controller != null
                && !__instance.currentLocation.farmers.Any()
                && (bool?) AccessTools.Field(typeof(Character), "freezeMotion").GetValue(__instance) is false)
            {
                __instance.WarpThroughLocationsUntilNoFarmers();
            }
        }
    }

    private static IEnumerable<CodeInstruction> Transpile_CharacterShouldCollideWithBuildingLayer(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        CodeMatcher matcher = new CodeMatcher(instructions, generator);
        matcher.Start();

        matcher.MatchStartForward(new CodeMatch(OpCodes.Ldc_I4_0), new CodeMatch(OpCodes.Ret));
        List<Label> labels = matcher.Labels;
        matcher.RemoveInstruction();

        matcher.Insert([
            new CodeInstruction(OpCodes.Ldarg_1),
            new CodeInstruction(OpCodes.Isinst, typeof(Farm)),
            new CodeInstruction(OpCodes.Ldnull),
            new CodeInstruction(OpCodes.Cgt_Un),
        ]);
        matcher.AddLabels(labels);
        

        foreach (var i in matcher.InstructionEnumeration())
        {
            Log.Trace(i.ToString());
            yield return i;
        }
    }
}
