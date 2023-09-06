using System;
using System.Collections.Generic;
using CollapseOnFarmFix.Framework;
using CollapseOnFarmFix.Framework.Patching;
using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace CollapseOnFarmFix
{
    public class ModEntry : Mod
    {
        internal new static IMonitor Monitor;
        internal static IModHelper ModHelper;
        internal new static IManifest ModManifest;

        internal static bool ToStartWakeUpEvent;
        internal static string PartnerNpc;
        internal static string WeatherLastNight;

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
            helper.Events.Content.AssetRequested += OnAssetRequested;

            
        }

        private void OnAssetRequested(object sender, AssetRequestedEventArgs e)
        {
            // CP additions will do "EditData" to add lines to this dictionary by
            // targeting "monsoonsheep.CollapseOnFarmFix/PutToBedDialogues"
            if (e.NameWithoutLocale.IsEquivalentTo($"Mods/{ModManifest.UniqueID}/PostPassoutDialogues"))
            {
                e.LoadFrom(() => new Dictionary<string, string>(), AssetLoadPriority.Medium);
            }

            else if (e.NameWithoutLocale.IsEquivalentTo("Data/Events/Hospital") 
                && Game1.player.currentLocation is Farm && Game1.killScreen is true
                && Game1.currentLocation.Name.Equals("Hospital"))
            {
                NPC partner = GetSpouseOrRoommate(Game1.player);

                if (!partner.Name.Equals("Harvey"))
                {
                    string eventString =
                        Game1.content.LoadStringReturnNullIfNotFound($"Mods/{ModManifest.UniqueID}/HospitalKilledEvent:{partner.Name}")
                        ?? Game1.content.LoadStringReturnNullIfNotFound($"Mods/{ModManifest.UniqueID}/HospitalKilledEvent:generic");
                    if (eventString == null)
                        return;
                    
                    e.Edit((d) => d.AsDictionary<string, string>().Data["PlayerKilled"] = string.Format(eventString, partner.Name));
                    Logger.Log("Replacing hospital death event");
                }
            }

            else if (e.NameWithoutLocale.IsEquivalentTo($"Mods/{ModManifest.UniqueID}/HospitalKilledEvent"))
            {
                e.LoadFrom(() => new Dictionary<string, string>(), AssetLoadPriority.Medium);
            }
        }

        private void OnDayStarted(object sender, DayStartedEventArgs e)
        {
            if (!ToStartWakeUpEvent) 
                return;

            NPC spouse = Game1.getCharacterFromName(PartnerNpc);
            if (spouse == null)
            {
                Logger.Log("Invalid information stored when passed out last night", LogLevel.Warn);
                ToStartWakeUpEvent = false;
                return;
            }

            Vector2 bedCoords = Game1.player.mostRecentBed;
            bedCoords.X = (int) bedCoords.X / 64;
            bedCoords.Y = (int) bedCoords.Y / 64;

            List<int> giftItems = new List<int>()
            {
                200, // vegetable medley
                199, // parsnip soup
                207, // bean hotpot
                211, // pancake
                218, // tom kha soup
                727, // chowder
            };

            int giftItemId = giftItems[Game1.random.Next(giftItems.Count)];

            string putToBedDialogue = Dialogues.GetPostPassoutDialogue(spouse, giftItemId);
            int friendshipWithSpouse = Game1.player.friendshipData[spouse.Name].Points / 250;
                
            string[] eventString = new[]
            {
                $"junimoStarSong/-100 -100/farmer {bedCoords.X} {bedCoords.Y} 3 {spouse.Name} {bedCoords.X-1} {bedCoords.Y} 1/",
                $"makeInvisible {bedCoords.X-2} {bedCoords.Y-2} 2 3/ignoreCollisions {spouse.Name}/",
                $"skippable/viewport {bedCoords.X} {bedCoords.Y}/pause 400/emote {spouse.Name} 16/pause 400/",
                $"speak {spouse.Name} \"{putToBedDialogue} [{giftItemId}]\"/pause 1000{(Game1.random.Next(friendshipWithSpouse) > 4 ? "/emote farmer 20" : "")}/end",
            };

            Event postPassoutEvent = new(string.Join(string.Empty, eventString));

            WeatherLastNight = null;
            ToStartWakeUpEvent = false;
            Game1.getLocationFromName(Game1.player.homeLocation.Value)?.startEvent(postPassoutEvent);
        }

        internal NPC GetSpouseOrRoommate(Farmer who)
        {
            if ((who.isMarried() || who.hasRoommate()) && who.spouse != null)
            {
                return Game1.getCharacterFromName(who.spouse); 
            }

            return null;
        }
    }
}
