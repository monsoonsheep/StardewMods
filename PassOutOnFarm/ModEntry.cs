using HarmonyLib;
using StardewModdingAPI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using PassOutOnFarm.Framework;
using PassOutOnFarm.Framework.Patching;
using StardewModdingAPI.Events;
using StardewValley;

namespace PassOutOnFarm
{
    public class ModEntry : Mod
    {
        internal new static IMonitor Monitor;
        internal static IModHelper ModHelper;
        internal new static IManifest ModManifest;

        internal static bool toStartWakeUpEvent = false;

        public override void Entry(IModHelper helper)
        {
            Monitor = base.Monitor;
            ModHelper = helper;
            ModManifest = base.ModManifest;
            Logger.Monitor = Monitor;
            try
            {
                var harmony = new Harmony(ModManifest.UniqueID);
                new PassingOutPatches().ApplyAll(harmony);
            }
            catch (Exception e)
            {
                Logger.Log($"Couldn't patch methods - {e}", LogLevel.Error);
                return;
            }
            Monitor = base.Monitor;
            ModHelper = helper;
            ModManifest = base.ModManifest;

            helper.Events.GameLoop.DayStarted += OnDayStarted;
        }

        private void OnDayStarted(object sender, DayStartedEventArgs e)
        {
            if (toStartWakeUpEvent)
            {
                NPC spouse = Game1.player.getSpouse();
                Vector2 bedCoords = Game1.player.mostRecentBed;
                bedCoords.X = (int) bedCoords.X / 64;
                bedCoords.Y = (int) bedCoords.Y / 64;

                Vector2 centerCoords = bedCoords;
                string locationName = Game1.player.homeLocation.Value;

                string[] eventString = new[]
                {
                    $"junimoStarSong/-100 -100/farmer {bedCoords.X} {bedCoords.Y} 3 {spouse.Name} {bedCoords.X-1} {bedCoords.Y} 1/makeInvisible {bedCoords.X-2} {bedCoords.Y-2} 2 3/",
                    $"ignoreCollisions {spouse.Name}/",
                    $"skippable/viewport {centerCoords.X} {centerCoords.Y}/pause 400/emote {spouse.Name} 16/pause 400/",
                    $"speak {spouse.Name} \"{Dialogues.GetPutToBedDialogue(spouse)}\"/pause 400/end",
                };

                Event putToBedEvent = new(string.Join(string.Empty, eventString))
                {
                    onEventFinished = () =>
                    {
                        DelayedAction.functionAfterDelay(
                            () =>
                            {
                                Game1.player.addItemByMenuIfNecessaryElseHoldUp(new StardewValley.Object(746, Game1.random.Next(2, 6)));
                            }, 100);
                    },
                };

                toStartWakeUpEvent = false;
                Game1.getLocationFromName(Game1.player.homeLocation.Value).startEvent(putToBedEvent);
            }
        }
    }
}
