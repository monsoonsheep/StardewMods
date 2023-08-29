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

        internal static string? GetDialogueFromTranslation(string key)
        {
            return Translator.Get(key).UsePlaceholder(false);
        }

        internal static string GetPutToBedDialogue(NPC npc)
        {
            string weather = "sunny";
            if (Game1.isRaining) weather = "rain";
            if (Game1.isSnowing) weather = "snow";
            if (Game1.isLightning) weather = "lightning";
            if (Game1.isDebrisWeather) weather = "debris";

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

            string? found = GetDialogueFromTranslation($"PutToBedDialogues.{npc.Name}.{weather}.1");


            if (string.IsNullOrEmpty(found))
                found = GetDialogueFromTranslation($"PutToBedDialogues.{npc.Name}.sunny.1");




            if (string.IsNullOrEmpty(found) || Game1.random.Next(5) == 0)
            {
                List<string> possibleEntries = new List<string>();
                possibleEntries.Add($"PutToBedDialogues.neutral.{weather}.1");

                if (manners == null)
                {
                    possibleEntries.Add($"PutToBedDialogues.{manners ?? "neutral"}.{weather}.1");
                }

                if (socialAnxiety == null)
                {
                    possibleEntries.Add($"PutToBedDialogues.{socialAnxiety ?? "neutral"}.{weather}.1");
                }

                // shuffle the second and third entries randomly
                if (possibleEntries.Count == 3 && Game1.random.Next(2) == 0)
                {
                    var tmp = possibleEntries[1];
                    possibleEntries.RemoveAt(1);
                    possibleEntries.Add(tmp);
                }

                foreach (string entry in possibleEntries)
                {
                    found = GetDialogueFromTranslation(entry);
                    if (string.IsNullOrEmpty(found) && !weather.Equals("sunny"))
                        found = GetDialogueFromTranslation(entry.Replace(weather, "sunny"));

                    if (!string.IsNullOrEmpty(found))
                    {
                        break;
                    }
                }
            }



            // Get alternates for the chosen type (suffixed by .2, .3 etc.)
            List<string> alternates = new List<string>() { };
            if (!string.IsNullOrEmpty(found))
            {
                for (int i = 2; i < 500; i++)
                {
                    string? alt = GetDialogueFromTranslation(found.Trim('1') + i);
                    if (string.IsNullOrEmpty(alt))
                        break;

                    alternates.Add(alt);
                }
            }
            else
            {
                Logger.Log("Couldn't find any suitable PutToBedDialogues. Defaulting.");
                found = "Hey, you're awake. You passed out like night and I had to carry you here.";
            }

            alternates.Add(found);
            return alternates[Game1.random.Next(alternates.Count)];
        }
    }
}
