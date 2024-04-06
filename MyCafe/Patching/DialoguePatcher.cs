using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using Microsoft.Xna.Framework;
using MonsoonSheep.Stardew.Common;
using MonsoonSheep.Stardew.Common.Patching;
using MyCafe.Characters;
using MyCafe.Locations.Objects;
using MyCafe.Netcode;
using StardewModdingAPI;
using StardewValley;

namespace MyCafe.Patching;
internal class DialoguePatcher : BasePatcher
{
    public override void Apply(Harmony harmony, IMonitor monitor)
    {
        harmony.Patch(
            original: this.RequireMethod<NPC>("loadCurrentDialogue"),
            postfix: this.GetHarmonyMethod(nameof(DialoguePatcher.After_NpcLoadCurrentDialogue))
        );
    }

    /// <summary>
    /// Daily dialogue load, adding Last Ate comment
    /// </summary>
    private static void After_NpcLoadCurrentDialogue(NPC __instance, ref Stack<Dialogue> __result)
    {
        if (Mod.Instance.VillagerData.TryGetValue(__instance.Name, out var data) &&
            data.LastVisitedDate.TotalDays > 1 &&
            data.LastAteFood != null &&
            Game1.random.Next(4) == 0 &&
            __result.Count <= 2)
        {
            Mod.Instance.TryAddDialogueLastAteComment(data, __result);
        }
    }
}
