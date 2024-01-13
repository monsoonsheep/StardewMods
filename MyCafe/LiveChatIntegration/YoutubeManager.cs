using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Google.Apis.YouTube.v3.Data;
using StreamingClient.Base.Model.OAuth;
using StreamingClient.Base.Util;
using YouTube.Base;
using YouTube.Base.Clients;
using LogLevel = StreamingClient.Base.Util.LogLevel;

namespace MyCafe;
public class YoutubeManager : IStreamManager
{
    private string _clientId;
    private string _clientSecret;

    private YouTubeConnection _connection;
    private ChatClient _chatClient;

    public event EventHandler<ChatMessageReceivedEventArgs> OnChatMessageReceived;

    public async Task<bool> Connect()
    {
        _clientId = ModConfig.LoadedConfig.YoutubeClientId;
        _clientSecret = ModConfig.LoadedConfig.YoutubeClientSecret;

        if (string.IsNullOrWhiteSpace(_clientId) || string.IsNullOrWhiteSpace(_clientSecret))
        {
            Log.Error("Error connecting to Youtube. Make sure to put your Client ID and Client Secret in the config.json file in the mod folder.");
            return false;
        }
        Logger.SetLogLevel(LogLevel.Error);
        Logger.LogOccurred += Logger_LogOccurred;

        try
        {
            List<OAuthClientScopeEnum> scopes =
            [
                OAuthClientScopeEnum.ManageData,
                OAuthClientScopeEnum.ManageVideos,
                OAuthClientScopeEnum.ReadOnlyAccount
            ];

            Log.Debug("Creating Youtube connection");
            
            _connection = await YouTubeConnection.ConnectViaLocalhostOAuthBrowser(_clientId, _clientSecret, scopes, true);

            if (_connection != null)
            {
                Channel channel = await _connection.Channels.GetMyChannel();
                if (channel != null)
                {
                    Log.Info($"Connection successful. Logged in as: {channel.Snippet.Title}");

                    var broadcast = await _connection.LiveBroadcasts.GetMyActiveBroadcast();
                    _chatClient = new ChatClient(_connection);
                    _chatClient.OnMessagesReceived += Client_OnMessagesReceived;

                    if (await _chatClient.Connect(broadcast))
                    {
                        Log.Info("Live chat connection successful!");

                        return true;
                    }
                    else
                    {
                        Log.Error("Error connecting to Youtube live broadcast!");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error("Error connecting to Youtube live!" + ex);
        }

        return false;
    }

    public async Task StartListening()
    {
        if (await _connection.LiveBroadcasts.GetMyActiveBroadcast() != null)
        {
            await _chatClient.SendMessage("Hello chat!");
        }

        while (true) { }
    }

    private void Client_OnMessagesReceived(object sender, IEnumerable<LiveChatMessage> messages)
    {
        if (OnChatMessageReceived == null)
            return;

        foreach (LiveChatMessage message in messages)
        {
            try
            {
                if (message.Snippet.HasDisplayContent.GetValueOrDefault())
                {
                    ChatMessageReceivedEventArgs args = new ChatMessageReceivedEventArgs();
                    args.Username = message.AuthorDetails.DisplayName;
                    args.Message = message.Snippet.DisplayMessage;
                    OnChatMessageReceived(this, args);
                }
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
        }
    }

    private void Logger_LogOccurred(object sender, StreamingClient.Base.Util.Log log)
    {
        if (log.Level <= LogLevel.Error)
        {
            Log.Debug(log.Message);
        }
    }


}