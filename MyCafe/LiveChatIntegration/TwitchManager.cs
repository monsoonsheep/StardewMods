using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Net.Http;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Web;
using System.Diagnostics;
using System.Data.Common;
using System.Threading.Channels;
using Twitch.Base.Clients;
using Twitch.Base;
using Twitch.Base.Models.Clients.Chat;
using Twitch.Base.Models.NewAPI.Users;

namespace MyCafe;
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
        OnChatMessageReceived?.Invoke(this, new ChatMessageReceivedEventArgs()
        {
            Username = e.UserDisplayName,
            Message = e.Message,
        });
    }

    public Task StartListening()
    {

        return Task.CompletedTask;
    }
}
