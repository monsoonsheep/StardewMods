using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewValley;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;

namespace CharGen.Patching;
internal class ExamplePatches : PatchCollection
{
    public ExamplePatches()
    {
        Patches = new List<Patch>
        {
            new (
                typeof(NPC),
                "update",
                new[] { typeof(GameTime), typeof(GameLocation) },
                postfix: ExamplePostfix),
        };
    }

    internal static void ExamplePostfix()
    {
        return;
    }
}
