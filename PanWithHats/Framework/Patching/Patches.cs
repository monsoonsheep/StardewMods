using HarmonyLib;
using StardewValley;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using Microsoft.Xna.Framework;
using StardewValley.Objects;
using StardewValley.Tools;
using Game1 = StardewValley.Game1;

namespace PanWithHats.Framework.Patching
{
    internal class Patches : PatchList
    {
        public Patches()
        {
            Patches = new List<Patch>
            {
                new (
                    typeof(Game1),
                    "pressUseToolButton",
                    null,
                    prefix: nameof(Before_PressUseToolButton),
                    postfix: nameof(After_PressUseToolButton)
                    ),

                new (
                    typeof(Farmer),
                    "useTool",
                    new[] { typeof(Farmer) },
                    prefix: nameof(Before_FarmerUseTool),
                    postfix: nameof(After_FarmerUseTool)
                    ),
                new (
                    typeof(Farmer),
                    "performBeginUsingTool",
                    null,
                    postfix: nameof(After_FarmerPerformBeginUsingTool)
                    ),
            };
        }

        public static void Before_PressUseToolButton(out Hat __state)
        {
            if (Game1.player.CurrentToolIndex < Game1.player.Items.Count && Game1.player.Items[Game1.player.CurrentToolIndex] is Hat hat)
            {
                __state = hat;
                Game1.player.CurrentTool = new Pan();
                ModEntry.UsingHatAsPan = true;
                ModEntry.HatPlayerWasHolding = __state;
            }

            __state = null;
        }

        public static void After_PressUseToolButton(Hat __state)
        {
            if (ModEntry.UsingHatAsPan && __state != null)
            {
                Game1.player.CurrentTool = null;
                ModEntry.HatPlayerWasHolding = __state;
                Game1.player.Items[Game1.player.CurrentToolIndex] = ModEntry.HatPlayerWasHolding;
            }
        }

        public static void After_FarmerPerformBeginUsingTool(Farmer __instance)
        {
            if (ModEntry.UsingHatAsPan && !Game1.player.UsingTool)
            {
                ModEntry.UsingHatAsPan = false;
                Game1.player.Items[Game1.player.CurrentToolIndex] = ModEntry.HatPlayerWasHolding;
            }
        }
        
        public static void Before_FarmerUseTool(Farmer __instance, Farmer who)
        {
            if (ModEntry.UsingHatAsPan)
            {
                Game1.player.CurrentTool = new Pan();
            }
        }

        public static void After_FarmerUseTool(Farmer __instance, Farmer who)
        {
            if (ModEntry.UsingHatAsPan)
            {
                Game1.player.Items[Game1.player.CurrentToolIndex] = ModEntry.HatPlayerWasHolding;
                //ModEntry.HatPlayerWasHolding = null;
                ModEntry.UsingHatAsPan = false;
            }
        }
    }
}
