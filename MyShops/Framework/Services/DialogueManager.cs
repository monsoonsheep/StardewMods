using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StardewMods.MyShops.Framework.Data;
using StardewValley.TokenizableStrings;

namespace StardewMods.MyShops.Framework.Services;
internal class DialogueManager
{
    internal static DialogueManager Instance = null!;

    internal DialogueManager()
        => Instance = this;

    internal void Initialize()
    {

    }

    internal void AddDialoguesOnArrivingAtCafe(NPC npc)
    {
        // Add the first time visit dialogues if their data's Last Visited value is the Spring 1, year 1
        string key = Mod.Instance.VillagerData[npc.Name].LastVisitedDate.TotalDays <= 1
            ? Values.MODASSET_DIALOGUE_ENTRY_CAFEFIRSTTIMEVISIT
            : Values.MODASSET_DIALOGUE_ENTRY_CAFEVISIT;

        KeyValuePair<string, string> entry = GetCustomDialogueAssetOrGeneric(npc, key);

        npc.CurrentDialogue.Push(
            new Dialogue(npc, $"{key}:{entry.Key}", TokenParser.ParseText(entry.Value, Game1.random, null, Game1.player))
            {
                removeOnNextMove = true,
                dontFaceFarmer = true
            }
        );
    }

    internal void TryAddDialogueLastAteComment(VillagerCustomerData npcData, Stack<Dialogue> dialogue)
    {
        KeyValuePair<string, string>? entry = GetCustomDialogueAsset(npcData.GetNpc(), Values.MODASSET_DIALOGUE_ENTRY_LASTATECOMMENT);

        if (entry.HasValue)
        {
            string text = TokenParser.ParseText(string.Format(entry.Value.Value, ItemRegistry.GetData(npcData.LastAteFood)?.DisplayName ?? "thing"), Game1.random, null, Game1.player);
            dialogue.Push(new Dialogue(npcData.GetNpc(), $"{Values.MODASSET_DIALOGUE_ENTRY_LASTATECOMMENT}:{entry.Value.Key}", text));
        }
    }

    internal static KeyValuePair<string, string> GetCustomDialogueAssetOrGeneric(NPC npc, string key)
    {
        Dictionary<string, string>? dialogueAsset = npc.Dialogue;
        if (!dialogueAsset?.ContainsKey(key) ?? false)
            dialogueAsset = Game1.content.Load<Dictionary<string, string>>("Data/ExtraDialogue")!;

        return dialogueAsset!.Where(pair => pair.Key.StartsWith(key)).ToList().PickRandom();
    }

    internal static KeyValuePair<string, string>? GetCustomDialogueAsset(NPC npc, string key)
    {
        try
        {
            return npc.Dialogue.Where(pair => pair.Key.StartsWith(key)).ToList().PickRandom();
        }
        catch
        {
            return null;
        }
    }
}
