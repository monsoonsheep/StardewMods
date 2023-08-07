using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FarmCafe.Framework.Characters;
using StardewValley;

namespace FarmCafe.Framework.Patching
{
    internal class GameLocationPatches : PatchList
    {
        public GameLocationPatches()
        {
            Patches = new List<Patch>
            {
                new (
                    typeof(GameLocation),
                    "cleanupBeforeSave",
                    null,
                    postfix: nameof(CleanupBeforeSavePostfix)),
            };
        }

        private static void CleanupBeforeSavePostfix(GameLocation __instance)
        {
            for (int i = __instance.characters.Count - 1; i >= 0; i--)
            {
                if (__instance.characters[i] is Customer)
                {
                    Debug.Log("Removing character before saving");
                    __instance.characters.RemoveAt(i);
                }
            }
        }
    }
}
