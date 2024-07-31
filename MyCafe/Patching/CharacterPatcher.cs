using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonsoonSheep.Stardew.Common;
using MonsoonSheep.Stardew.Common.Patching;
using MyCafe.Characters;
using MyCafe.Data.Customers;
using MyCafe.Game;
using MyCafe.Locations.Objects;
using Netcode;
using StardewModdingAPI;
using StardewValley;
using SUtility = StardewValley.Utility;

namespace MyCafe.Patching;

[SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Harmony patching requirement")]
internal class CharacterPatcher : BasePatcher
{
    public override void Apply(Harmony harmony, IMonitor monitor)
    {
        harmony.Patch(
            original: this.RequireMethod<NPC>(nameof(NPC.draw), [typeof(SpriteBatch), typeof(float)]),
            transpiler: this.GetHarmonyMethod(nameof(CharacterPatcher.Transpile_NpcDraw))
        );
        harmony.Patch(
            original: this.RequireMethod<NPC>(nameof(NPC.ChooseAppearance)),
            prefix: this.GetHarmonyMethod(nameof(CharacterPatcher.Before_ChooseAppearance))
        );
        harmony.Patch(
            original: this.RequireMethod<NPC>(nameof(NPC.update), [typeof(GameTime), typeof(GameLocation)]),
            postfix: this.GetHarmonyMethod(nameof(CharacterPatcher.After_update))
        );
    }

    /// <summary>
    /// Adjust the draw layer of NPC when drawing if they are a customer sitting down
    /// </summary>
    private static IEnumerable<CodeInstruction> Transpile_NpcDraw(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        CodeMatcher matcher = new CodeMatcher(instructions, generator);
        matcher.MatchEndForward(new CodeMatch(OpCodes.Ldloc_0));
        matcher.MatchEndForward(new CodeMatch(OpCodes.Conv_R4));
        matcher.Advance(1);

        Label l1 = generator.DefineLabel();

        matcher.AddLabels([l1]);

        // Add 10f to the drawLayer if NPC is sitting down
        matcher.Insert([
            new CodeInstruction(OpCodes.Ldarg_0),
            CodeInstruction.Call(typeof(NpcVirtualProperties), nameof(NpcVirtualProperties.get_IsSittingDown), [typeof(NPC)]),
            CodeInstruction.Call(typeof(NetBool), "get_Value", []),
            new CodeInstruction(OpCodes.Brfalse_S, l1),
            new CodeInstruction(OpCodes.Ldc_R4, 10f),
            new CodeInstruction(OpCodes.Add)
        ]);

        return matcher.InstructionEnumeration();
    }

    /// <summary>
    /// The game tries to load appearance for random NPC customers and fails, we block the method if the NPC's name starts with the prefix
    /// </summary>
    private static bool Before_ChooseAppearance(NPC __instance, LocalizedContentManager content)
    {
        return !__instance.Name.StartsWith(ModKeys.CUSTOMER_NPC_NAME_PREFIX);
    }

    private static void After_update(NPC __instance, GameTime time, GameLocation location)
    {
        // Run only if either villager customer (in the hashset) or random customer (name starts with a prefix)
        if (!Context.IsMainPlayer || !(Mod.Cafe.NpcCustomers.Contains(__instance.Name) || __instance.Name.StartsWith(ModKeys.CUSTOMER_NPC_NAME_PREFIX)))
            return;

        #if DEBUG
        __instance.speed = 4;
        #endif

        // If they are in Bus Stop or town and there's no farmers there, warp them to the next location in their route
        if (Mod.Config.WarpCustomers
            && __instance.controller != null
            && !__instance.currentLocation.farmers.Any()
            && (bool?) AccessTools.Field(typeof(Character), "freezeMotion").GetValue(__instance) is false)
        {
            __instance.WarpThroughLocationsUntilNoFarmers();
        }

        // Lerping the position of the character (for sitting and getting up)
        if (__instance.get_LerpPosition() >= 0f)
        {
            __instance.set_LerpPosition(__instance.get_LerpPosition() + (float)time.ElapsedGameTime.TotalSeconds);

            if (__instance.get_LerpPosition() >= __instance.get_LerpDuration())
            {
                __instance.set_LerpPosition(__instance.get_LerpDuration());
            }

            __instance.Position = new Vector2(SUtility.Lerp(__instance.get_LerpStartPosition().X, __instance.get_LerpEndPosition().X, __instance.get_LerpPosition() / __instance.get_LerpDuration()), StardewValley.Utility.Lerp(__instance.get_LerpStartPosition().Y, __instance.get_LerpEndPosition().Y, __instance.get_LerpPosition() / __instance.get_LerpDuration()));
            if (__instance.get_LerpPosition() >= __instance.get_LerpDuration())
            {
                __instance.get_AfterLerp()?.Invoke(__instance);
                __instance.set_AfterLerp(null);
                __instance.set_LerpPosition(-1f);
            }
        }
       
    }
}
