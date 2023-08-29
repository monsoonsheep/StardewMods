#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StardewModdingAPI;
using StardewValley;

namespace PassOutOnFarm.Framework
{
    
    internal class Dialogues
    {
        internal static ITranslationHelper Translator = ModEntry.ModHelper.Translation;

        internal static string? GetBedDialogueFromTranslation(string key)
        {
            return Translator.Get($"PutToBedDialogues.{key}").UsePlaceholder(false);
        }

        internal static string GetPutToBedDialogue(NPC npc, int giftItemId)
        {
            string weather = "sunny";
            if (Game1.isRaining) weather = "rain";
            if (Game1.isSnowing) weather = "snow";
            if (Game1.isLightning) weather = "lightning";
            if (Game1.isDebrisWeather) weather = "debris";

            Item giftItem = new StardewValley.Object(giftItemId, 1);

            string? manners = npc.Manners switch
            {
                1 => "polite",
                2 => "rude",
                _ => null
            };
            string? socialAnxiety = npc.SocialAnxiety switch
            {
                1 => "outgoing",
                2 => "shy",
                _ => null
            };

            // find "name.weather.1", then "name.sunny.1", then "personality.weather.1", then "personality.sunny.1"

            string? found = GetBedDialogueFromTranslation($"{npc.Name}.{weather}.1");


            if (string.IsNullOrEmpty(found))
                found = GetBedDialogueFromTranslation($"{npc.Name}.sunny.1");

            if (string.IsNullOrEmpty(found) || Game1.random.Next(5) == 0)
            {
                List<string> possibleEntries = new List<string>();
                possibleEntries.Add($"neutral.{weather}.1");

                // add key for rude or polite
                if (manners != null)
                {
                    possibleEntries.Add($"{manners}.{weather}.1");
                }

                // add key for shy or outgoing
                if (socialAnxiety != null)
                {
                    possibleEntries.Add($"{socialAnxiety}.{weather}.1");
                }
                // The weather added will be replaced by sunny if that weather isn't found.

                // shuffle the second and third entries randomly (if there are 3)
                if (possibleEntries.Count == 3 && Game1.random.Next(2) == 0)
                {
                    var tmp = possibleEntries[1];
                    possibleEntries.RemoveAt(1);
                    possibleEntries.Add(tmp);
                }

                foreach (string entry in possibleEntries)
                {
                    found = GetBedDialogueFromTranslation(entry);
                    if (string.IsNullOrEmpty(found) && !weather.Equals("sunny"))
                        found = GetBedDialogueFromTranslation(entry.Replace(weather, "sunny"));

                    if (!string.IsNullOrEmpty(found))
                        break;
                }
            }



            // Get alternates for the chosen type (suffixed by .2, .3 etc.)
            List<string> alternates = new List<string>() { };
            if (!string.IsNullOrEmpty(found))
            {
                for (int i = 2; i < 500; i++)
                {
                    string? alt = GetBedDialogueFromTranslation(found.Trim('1') + i);
                    if (string.IsNullOrEmpty(alt))
                        break;

                    alternates.Add(alt);
                }
            }
            else
            {
                Logger.Log("Couldn't find any suitable PutToBedDialogues. Defaulting.");
                found = "Hey, you're awake. You passed out last night.#$b#You shouldn't push yourself like that.";
            }

            alternates.Add(found);
            return alternates[Game1.random.Next(alternates.Count)];
        }
    }
}
