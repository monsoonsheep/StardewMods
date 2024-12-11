using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StardewMods.FoodJoints.Framework.Services;

internal class MultiplayerManager
{
    internal static MultiplayerManager Instance = null;

    internal MultiplayerManager()
        => Instance = this;

    internal void Initialize()
    {
        Mod.Events.Multiplayer.ModMessageReceived += this.OnModMessageReceived;
    }

    internal void OnModMessageReceived(object? sender, ModMessageReceivedEventArgs e)
    {
        if (e.FromModID != Mod.Manifest.UniqueID)
            return;

        if (e.Type == "CustomerDoEmote" && !Context.IsMainPlayer)
        {
            try
            {
                (string key, int emote) = e.ReadAs<(string, int)>();
                Game1.getCharacterFromName(key)?.doEmote(emote);
            }
            catch (InvalidOperationException ex)
            {
                Log.Debug($"Invalid message from host\n{ex}", LogLevel.Error);
            }
        }
    }
}
