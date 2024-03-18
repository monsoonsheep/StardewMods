using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MyCafe;
using MyCafe.LiveChatIntegration;
using MyCafe.Enums;
using MyCafe.Locations.Objects;
using MyCafe.UI.Options;
using StardewModdingAPI;
using StardewValley;
using Color = Microsoft.Xna.Framework.Color;
using LogLevel = StreamingClient.Base.Util.LogLevel;
using Point = Microsoft.Xna.Framework.Point;
using Rectangle = Microsoft.Xna.Framework.Rectangle;

#if TWITCH
using Twitch.Base;
using Twitch.Base.Clients;
using Twitch.Base.Models.Clients.Chat;
using Twitch.Base.Models.NewAPI.Users;

#elif YOUTUBE
using YouTube.Base.Clients;
using YouTube.Base;
using Google.Apis.YouTube.v3.Data;
using StreamingClient.Base.Util;
#endif

// ReSharper disable once CheckNamespace

namespace MyCafe.LiveChatIntegration
{
    internal interface IStreamManager
    {
        public event EventHandler<ChatMessageReceivedEventArgs> ChatMessageReceived;
        public Task<bool> Connect();
    }

    public class ChatMessageReceivedEventArgs : EventArgs
    {
        public string Username { get; set; } = null!;
        public string Message { get; set; } = null!;
        public Color Color { get; set; } = Color.White;
    }

    internal enum ConnectionStatus
    {
        Disconnected,
        Disconnecting,
        Connecting,
        Connected
    }

    #if TWITCH
    internal class TwitchManager : IStreamManager
    {
        private string _clientId = null!;
        private string _clientSecret = null!;

        private TwitchConnection? _connection;
        private static UserModel? user;
        private static ChatClient? chat;
        private ConnectionStatus _connectionState = ConnectionStatus.Disconnected;

        public event EventHandler<ChatMessageReceivedEventArgs>? ChatMessageReceived;
        private readonly ColorConverter colorConverter = new ();

        public async Task<bool> Connect()
        {
            this._clientId = Mod.Config.TwitchClientId;
            this._clientSecret = Mod.Config.TwitchClientSecret;

            try
            {
                Log.Info($"Twitch - Connecting...");
                this._connectionState = ConnectionStatus.Connecting;

                List<OAuthClientScopeEnum> scopes =
                [
                    OAuthClientScopeEnum.chat__read,
                    OAuthClientScopeEnum.chat__edit,
                    OAuthClientScopeEnum.whispers__read,
                ];

                this._connection = await TwitchConnection.ConnectViaLocalhostOAuthBrowser(this._clientId, this._clientSecret, scopes, forceApprovalPrompt: true);

                if (this._connection != null)
                {
                    Log.Info($"Twitch - Connection successful");
                    user = await this._connection.NewAPI.Users.GetCurrentUser();
                    if (user != null)
                    {
                        chat = new ChatClient(this._connection);
                        chat.OnMessageReceived += this.Chat_OnMessageReceived;

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

            this._connectionState = ConnectionStatus.Connected;
            return true;
        }

        private void Chat_OnMessageReceived(object? sender, ChatMessagePacketModel e)
        {
            var color = (System.Drawing.Color)(this.colorConverter.ConvertFromInvariantString(e.Color) ?? System.Drawing.Color.White);
            Color resultColor = new Color(color.R, color.G, color.B);
            this.ChatMessageReceived?.Invoke(this, new ChatMessageReceivedEventArgs()
            {
                Username = e.UserDisplayName,
                Message = e.Message,
                Color = resultColor
            });
        }
    }

    #elif YOUTUBE
    public class YoutubeManager : IStreamManager
    {
        private string? _clientId;
        private string? _clientSecret;

        private YouTubeConnection? _connection;
        private ChatClient? _chatClient;

        public event EventHandler<ChatMessageReceivedEventArgs>? ChatMessageReceived;

        public async Task<bool> Connect()
        {
            this._clientId = Mod.Config.YoutubeClientId;
            this._clientSecret = Mod.Config.YoutubeClientSecret;

            if (string.IsNullOrWhiteSpace(this._clientId) || string.IsNullOrWhiteSpace(this._clientSecret))
            {
                Log.Error("Error connecting to Youtube. Make sure to put your Client ID and Client Secret in the config.json file in the mod folder.");
                return false;
            }

            Logger.SetLogLevel(LogLevel.Error);
            Logger.LogOccurred += this.Logger_LogOccurred;

            try
            {
                List<OAuthClientScopeEnum> scopes =
                [
                    OAuthClientScopeEnum.ManageData,
                    OAuthClientScopeEnum.ManageVideos,
                    OAuthClientScopeEnum.ReadOnlyAccount
                ];

                Log.Debug("Creating Youtube connection");

                this._connection = await YouTubeConnection.ConnectViaLocalhostOAuthBrowser(this._clientId, this._clientSecret, scopes, true);

                if (this._connection != null)
                {
                    Channel channel = await this._connection.Channels.GetMyChannel();
                    if (channel != null)
                    {
                        Log.Info($"Connection successful. Logged in as: {channel.Snippet.Title}");

                        var broadcast = await this._connection.LiveBroadcasts.GetMyActiveBroadcast();
                        this._chatClient = new ChatClient(this._connection);
                        this._chatClient.OnMessagesReceived += this.Client_OnMessagesReceived;

                        if (await this._chatClient.Connect(broadcast))
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

        private void Client_OnMessagesReceived(object? sender, IEnumerable<LiveChatMessage> messages)
        {
            if (this.ChatMessageReceived == null)
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
                        this.ChatMessageReceived(this, args);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log(ex);
                }
            }
        }

        private void Logger_LogOccurred(object? sender, StreamingClient.Base.Util.Log log)
        {
            if (log.Level <= LogLevel.Error)
            {
                Log.Debug(log.Message);
            }
        }
    }
    #endif
}

namespace MyCafe.UI
{
    internal class ChatIntegrationPage : OptionsPageBase
    {
        public ChatIntegrationPage(CafeMenu parentMenu, Rectangle bounds, Texture2D sprites) : base("Chat Integration", bounds, parentMenu)
        {
            this.Options.Add(
                new OptionStatusSet("Status", "Connect", "Not connected.", "Connected!", this.ConnectChat, this.IsConnected, this.OptionSlotSize, 43490));
        }

        internal void ConnectChat()
        {
            if (this.IsConnected() || this.IsConnecting())
                return;
            
            Mod.Instance.InitializeLiveChat();
        }

        internal bool IsConnected()
        {
            return Mod.ChatManager.State == ChatConnectionState.On;
        }

        internal bool IsConnecting()
        {
            return Mod.ChatManager.State == ChatConnectionState.Connecting;
        }
    }
}

namespace MyCafe
{
    internal class LiveChatManager
    {
        private readonly IStreamManager _streamManager;

        private readonly List<string> _users = [];
        private readonly List<string> _spawnedUsers = [];
        private Task<bool> connectTask = null!;

        internal ChatConnectionState State;

        public LiveChatManager()
        {
#if YOUTUBE
            this._streamManager = new YoutubeManager();
#elif TWITCH
            this._streamManager = new TwitchManager();
#endif
        }

        internal void Initialize(IModHelper helper)
        {
            this.State = ChatConnectionState.Connecting;

            this.connectTask = this._streamManager.Connect();
            this.connectTask.ContinueWith((task) =>
            {
                if (task.Result)
                {
                    Mod.ChatManager._streamManager.ChatMessageReceived += Mod.ChatManager.OnChatMessageReceived;
                    Game1.chatBox.addMessage("Live chat connected!", Color.White);
                    Mod.ChatManager.State = ChatConnectionState.On;
                }
                else
                {
                    Mod.ChatManager.State = ChatConnectionState.Off;
                }
            });
        }

        internal void OnChatMessageReceived(object? sender, ChatMessageReceivedEventArgs e)
        {
            Log.Trace("Chat message " + e.Username + ": " + e.Message);
            if (!this._users.Contains(e.Username))
                this._users.Add(e.Username);
        }
    }
}

namespace MyCafe.Enums
{
    internal enum ChatConnectionState
    {
        Off, Connecting, On
    }
}
