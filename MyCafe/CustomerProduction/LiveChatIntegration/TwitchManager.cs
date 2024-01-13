using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using StardewValley;
using Twitch.Base;
using Twitch.Base.Clients;
using Twitch.Base.Models.Clients.Chat;
using Twitch.Base.Models.NewAPI.Users;
using Color = Microsoft.Xna.Framework.Color;

namespace MyCafe.CustomerProduction;
internal class TwitchManager : IStreamManager
{
    private string _clientId;
    private string _clientSecret;

    private enum ConnectionStatus
    {
        Disconnected,
        Disconnecting,
        Connecting,
        Connected
    }

    private TwitchConnection _connection;
    private static UserModel user;
    private static ChatClient chat;
    private ConnectionStatus _connectionState = ConnectionStatus.Disconnected;

    public event EventHandler<ChatMessageReceivedEventArgs> OnChatMessageReceived;
    private ColorConverter colorConverter;


    public async Task<bool> Connect()
    {
        _clientId = ModConfig.LoadedConfig.TwitchClientId;
        _clientSecret = ModConfig.LoadedConfig.TwitchClientSecret;

        try
        {
            Log.Info($"Twitch - Connecting to irc.chat.twitch.tv:6667");
            _connectionState = ConnectionStatus.Connecting;

            List<OAuthClientScopeEnum> scopes =
            [
                OAuthClientScopeEnum.chat__read,
                OAuthClientScopeEnum.chat__edit,
                OAuthClientScopeEnum.whispers__read,

            ];
            Log.Debug("Creating Youtube connection");

            _connection = await TwitchConnection.ConnectViaLocalhostOAuthBrowser(_clientId, _clientSecret, scopes, forceApprovalPrompt: false);

            if (_connection != null)
            {
                Log.Info($"Connection successful");
                user = await _connection.NewAPI.Users.GetCurrentUser();
                if (user != null)
                {
                    chat = new ChatClient(_connection);
                    chat.OnMessageReceived += Chat_OnMessageReceived;

                    await chat.Connect();
                    await Task.Delay(1000);
                    await chat.Join(user);
                    await Task.Delay(2000);
                }
            }
            else
            {
                throw new Exception();
            }
        }
        catch (Exception ex)
        {
            Log.Error("Twitch - Connection failed! " + ex.Message);
            return false;
        }

        _connectionState = ConnectionStatus.Connected;
        return true;
    }

    private void Chat_OnMessageReceived(object sender, ChatMessagePacketModel e)
    {
        var color = (System.Drawing.Color) (colorConverter.ConvertFromInvariantString(e.Color) ?? System.Drawing.Color.White);
        Color resultColor = new Color(color.R, color.G, color.B);
        OnChatMessageReceived?.Invoke(this, new ChatMessageReceivedEventArgs()
        {
            Username = e.UserDisplayName,
            Message = e.Message,
            Color = resultColor
        });
    }
}
