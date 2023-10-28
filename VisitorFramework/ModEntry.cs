using VisitorFramework.Framework.Managers;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using static VisitorFramework.Framework.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using VisitorFramework.Framework;
using VisitorFramework.Framework.Characters;
using Rectangle = Microsoft.Xna.Framework.Rectangle;
using Object = StardewValley.Object;
using VisitorFramework.Patching;
using StardewValley.Locations;
using System.Reflection;
using StardewValley.Tools;
using VisitorFramework;

namespace VisitorFramework
{
    /// <summary>The mod entry point.</summary>
    internal sealed class ModEntry : Mod
    {
        internal new static IMonitor Monitor;
        internal static IModHelper ModHelper;
        internal new static IManifest ModManifest;

        /// <inheritdoc/>
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
                new GameLocationPatches().ApplyAll(harmony);
                new CharacterPatches().ApplyAll(harmony);
                new UtilityPatches().ApplyAll(harmony);
                new FurniturePatches().ApplyAll(harmony);
            }
            catch (Exception e)
            {
                Logger.Log($"Couldn't patch methods - {e}", LogLevel.Error);
                return;
            }

            helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
            helper.Events.GameLoop.DayStarted += OnDayStarted;
            helper.Events.GameLoop.DayEnding += OnDayEnding;

            helper.Events.GameLoop.TimeChanged += OnTimeChanged;
            helper.Events.Input.ButtonPressed += Debug.ButtonPress;
        }

        private static void OnSaveLoaded(object sender, SaveLoadedEventArgs e)
        {
            if (!Context.IsMainPlayer)
                return;
            
            AssetManager.LoadVisitorModels(ModHelper, ref VisitorManager.VisitorModels);
            AssetManager.LoadContentPacks(ModHelper, ref VisitorManager.VisitorModels);
        }

        private static void OnDayStarted(object sender, DayStartedEventArgs e)
        {
            BusManager.UpdateBusStopLocation();

            if (!Context.IsMainPlayer)
                return;

            if (Game1.MasterPlayer.mailReceived.Contains("ccVault"))
            {
                BusManager.BusDepartureTimes = new[] { 1110, 1430, 1800 };
            }
        }

        private static void OnDayEnding(object sender, DayEndingEventArgs e)
        {
            if (Context.IsMainPlayer)
            {
                VisitorManager.RemoveAllVisitors();
            }
        }

        private static void OnTimeChanged(object sender, TimeChangedEventArgs e)
        {
            if (!Context.IsMainPlayer)
                return;

            // Bus departure
            if (BusManager.BusDeparturesToday < BusManager.BusDepartureTimes.Length && !BusManager.BusGone)
            {
                if (e.NewTime == BusManager.BusDepartureTimes[BusManager.BusDeparturesToday])
                {
                    NPC pam = Game1.getCharacterFromName("Pam");
                    if (BusManager.BusLocation.characters.Contains(pam) && pam.TilePoint is { X: 11, Y: 10 })
                    {
                        BusManager.BusLeave();
                    }
                }
            }

            // Bus arrival
            if (BusManager.BusGone)
            {
                int minutes = BusManager.MinutesSinceBusLeft += 10;
                if ((minutes >= 30 && Game1.random.Next(4) == 0) || minutes >= 60)
                {
                    BusManager.BusReturn();
                }
            }
        }

    }
}