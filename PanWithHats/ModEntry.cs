using HarmonyLib;
using PanWithHats.Framework.Patching;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using System;
using System.Diagnostics;
using StardewValley.Objects;
using Patches = PanWithHats.Framework.Patching.Patches;

namespace PanWithHats
{
    public class ModEntry : Mod
    {
        internal new static IMonitor Monitor;
        internal static IModHelper ModHelper;
        internal new static IManifest ModManifest;

        internal static bool UsingHatAsPan = false;
        internal static Hat HatPlayerWasHolding = null;


        public override void Entry(IModHelper helper)
        {
            Monitor = base.Monitor;
            ModHelper = helper;
            ModManifest = base.ModManifest;
            Logger.Monitor = Monitor;

            // Harmony patches
            try
            {
                var harmony = new Harmony(ModManifest.UniqueID);
                new Patches().ApplyAll(harmony);
            }
            catch (Exception e)
            {
                Logger.Log($"Couldn't patch methods - {e}", LogLevel.Error);
                return;
            }

#if DEBUG
            helper.Events.Input.ButtonPressed += Debug.ButtonPress;
#endif

            helper.Events.Content.AssetRequested += OnAssetRequested;
        }

        private void OnAssetRequested(object sender, AssetRequestedEventArgs e)
        {

        }
    }
}
