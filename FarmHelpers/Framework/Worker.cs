using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using StardewMods.FarmHelpers.Framework.Enums;

namespace StardewMods.FarmHelpers.Framework;
internal class Worker
{
    internal static Worker Instance = null!;

    internal WorkerState State = WorkerState.OffDuty;

    internal NPC? Npc;

    internal Worker()
    {
        Instance = this;

        Mod.Events.GameLoop.SaveLoaded += this.OnSaveLoaded;

        Mod.Harmony.Patch(
            AccessTools.Method(typeof(Character), nameof(Character.collideWith), [typeof(StardewValley.Object)]),
            postfix: new HarmonyMethod(this.GetType(), nameof(After_CharacterCollideWith))
            );
    }

    private static void After_CharacterCollideWith(Character __instance, StardewValley.Object o, ref bool __result)
    {
        if (__instance == Instance.Npc && o is Fence fence && fence.isGate.Value)
        {
            __result = false;
        }
    }

    private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
    {
        foreach (var pair in Game1.characterData)
        {
            if (pair.Value.CustomFields != null
                && pair.Value.CustomFields.TryGetValue("Mods/MonsoonSheep.FarmHelpers/HelperNpc", out string? val)
                && val.ToLower() == "true")
            {
                this.Npc = Game1.getCharacterFromName(pair.Key);
            }
        }
    }
}
