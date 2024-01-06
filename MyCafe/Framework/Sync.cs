using Microsoft.Xna.Framework;
using MyCafe.Framework.Managers;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using MyCafe.Framework.ChairsAndTables;

namespace MyCafe.Framework;

internal static class Sync
{
    internal static void OnPeerConnected(object sender, PeerConnectedEventArgs e)
    {
        return;
    }

    internal static void OnModMessageReceived(object sender, ModMessageReceivedEventArgs e)
    {
        if (e.FromModID != Mod.ModManifest.UniqueID)
            return;

        if (e.Type == "ClickTable" && Context.IsMainPlayer)
        {
            try
            {
                var data = e.ReadAs<KeyValuePair<string, string>>();

                Farmer who = Game1.getFarmer(long.Parse(data.Key));
                var matches = Regex.Matches(data.Value, @"\d+");
                if (who != null && matches.Count == 2)
                {
                    Table table = TableManager.Instance.GetTableAt(who.currentLocation, new Vector2(float.Parse(matches[0].Value), float.Parse(matches[1].Value)));
                    if (table != null)
                        TableManager.Instance.FarmerClickTable(table, who);
                }
            }
            catch
            {
                Log.Debug("Invalid message from host", LogLevel.Warn);
            }
        }
        else if (e.Type == "VisitorDoEmote" && !Context.IsMainPlayer)
        {
            try
            {
                var info = e.ReadAs<KeyValuePair<string, int>>();
                NPC npc = Game1.getCharacterFromName(info.Key);
                int emote = info.Value;

                npc?.doEmote(emote);
            }
            catch
            {
                Log.Debug("Invalid message from host", LogLevel.Warn);
            }
        }
    }

    internal static void SendTableClick(Table table, Farmer who)
    {
        Mod.ModHelper.Multiplayer.SendMessage(
            message: new KeyValuePair<string, string>(who.UniqueMultiplayerID.ToString(), table.Position.ToString()),
            messageType: "ClickTable",
            modIDs: new[] { Mod.ModManifest.UniqueID });
    }
}