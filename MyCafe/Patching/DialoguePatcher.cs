using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using MonsoonSheep.Stardew.Common.Patching;
using MyCafe.Characters;
using MyCafe.Netcode;
using StardewModdingAPI;
using StardewValley;

namespace MyCafe.Patching;
internal class DialoguePatcher : BasePatcher
{
    public override void Apply(Harmony harmony, IMonitor monitor)
    {
        //harmony.Patch(
        //    original: this.RequireMethod<NPC>(nameof(NPC.checkForNewCurrentDialogue)),
        //    postfix: this.GetHarmonyMethod(nameof(DialoguePatcher.After_NpcCheckForNewCurrentDialogue))
        //);
    }

    private static void After_NpcCheckForNewCurrentDialogue(NPC __instance)
    {
        if (Mod.Cafe.NpcCustomers.Contains(__instance.Name) && __instance.get_IsSittingDown())
        {
            
        }
    }
}
