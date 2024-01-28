﻿using HarmonyLib;
using Microsoft.Xna.Framework;
using MyCafe.Customers;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Pathfinding;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using MonsoonSheep.Stardew.Common.Patching;
using MyCafe.Locations;
using Object = StardewValley.Object;

namespace MyCafe.Patching;

internal class CharacterPatcher : BasePatcher
{
    public override void Apply(Harmony harmony, IMonitor monitor)
    {
        harmony.Patch(
            original: this.RequireMethod<PathFindController>("moveCharacter"),
            transpiler: this.GetHarmonyMethod(nameof(CharacterPatcher.Transpile_MoveCharacter))
        );
        harmony.Patch(
            original: this.RequireMethod<NPC>("ChooseAppearance"),
            prefix: this.GetHarmonyMethod(nameof(CharacterPatcher.Before_ChooseAppearance))
        );
        harmony.Patch(
            original: this.RequireMethod<Character>(nameof(Character.doEmote), [typeof(int), typeof(bool), typeof(bool)]),
            postfix: this.GetHarmonyMethod(nameof(CharacterPatcher.After_DoEmote))
        );
    }

    private static bool Before_ChooseAppearance(NPC __instance, LocalizedContentManager content)
    {
        if (__instance is Customer c && c.Name.StartsWith("CustomerNPC"))
            return false;

        return true;
    }

    private static IEnumerable<CodeInstruction> Transpile_MoveCharacter(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        var fPathfindercharacter = AccessTools.Field(typeof(PathFindController), "character");
        var mObjectIsPassableMethod = AccessTools.Method(typeof(Object), "isPassable");

        var codeList = instructions.ToList();
        int startPos = -1;

        for (int i = 0; i < codeList.Count; i++)
        {
            if (codeList[i].Calls(mObjectIsPassableMethod))
            {
                startPos = i + 1;
                break;
            }
        }

        if (codeList[startPos].Branches(out var jumpLabel) && jumpLabel != null)
        {
            var patchCodes = new List<CodeInstruction>
            {
                new (OpCodes.Ldarg_0), // this
                new (OpCodes.Ldfld, fPathfindercharacter), // this.character
                new (OpCodes.Isinst, typeof(Customer)),
                new (OpCodes.Brtrue, jumpLabel) // branch to the same one found earlier
            };

            codeList.InsertRange(startPos + 1, patchCodes);
        }
        else
            Log.Debug("Couldn't find the break after isPassable check", LogLevel.Error);

        return codeList.AsEnumerable();
    }

    private static void After_DoEmote(Character __instance, int whichEmote, bool playSound, bool nextEventCommand)
    {
        if (__instance is Customer c && Context.IsMainPlayer)
        {
            // send emote command to clients
        }
    }
}