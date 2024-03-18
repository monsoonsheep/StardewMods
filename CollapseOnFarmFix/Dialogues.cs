#nullable enable
using System.Collections.Generic;
using StardewModdingAPI;
using StardewValley;

namespace CollapseOnFarmFix.Framework
{
    
    internal class Dialogues
    {
        internal static string GetBedDialogue(string key)
        {
            return Game1.content.Load<Dictionary<string, string>>($"Mods/{Mod.Instance.ModManifest.UniqueID}/PostPassoutDialogues").TryGetValue(key, out string? result) ? result : string.Empty;
        }

        internal static string GetPostPassoutDialogue(NPC npc, int giftItemId)
        {
            string weather = Mod.WeatherLastNight ?? "sunny";

            string manners = npc.Manners switch
            {
                1 => "polite",
                2 => "rude",
                _ => string.Empty
            };
            string socialAnxiety = npc.SocialAnxiety switch
            {
                1 => "outgoing",
                2 => "shy",
                _ => string.Empty
            };

            // find "name.weather.1", then "name.sunny.1", then "personality.weather.1", then "personality.sunny.1"

            string? found = GetBedDialogue($"{npc.Name}.{weather}.1");

            if (string.IsNullOrEmpty(found))
                found = GetBedDialogue($"{npc.Name}.sunny.1");

            if (string.IsNullOrEmpty(found) || Game1.random.Next(20) == 0)
            {
                List<string> possibleEntries = new List<string>();

                // add key for neutral personality
                possibleEntries.Add($"neutral.{weather}.1");

                // add key for rude or polite
                if (!string.IsNullOrEmpty(manners))
                    possibleEntries.Add($"{manners}.{weather}.1");
                
                // add key for shy or outgoing
                if (!string.IsNullOrEmpty(socialAnxiety))
                    possibleEntries.Add($"{socialAnxiety}.{weather}.1");
                
                // The weather added will be replaced by sunny if that weather's entry isn't found.

                // shuffle the second and third entries randomly (if there are 3)
                if (possibleEntries.Count == 3 && Game1.random.Next(2) == 0)
                {
                    string tmp = possibleEntries[1];
                    possibleEntries.RemoveAt(1);
                    possibleEntries.Add(tmp);
                }

                foreach (string entry in possibleEntries)
                {
                    found = GetBedDialogue(entry);
                    if (string.IsNullOrEmpty(found) && !weather.Equals("sunny"))
                        found = GetBedDialogue(entry.Replace(weather, "sunny"));

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
                    string? alt = GetBedDialogue(found.Trim('1') + i);
                    if (string.IsNullOrEmpty(alt))
                        break;

                    alternates.Add(alt);
                }
            }
            else
            {
                Log.Debug("Couldn't find any suitable PutToBedDialogues. Defaulting.", LogLevel.Debug);
                found = "Hey, you're awake. You passed out last night.#$b#You shouldn't push yourself like that.";
            }

            alternates.Add(found);
            return alternates[Game1.random.Next(alternates.Count)];
        }
    }
}
