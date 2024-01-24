using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MyCafe.Customers;
using MyCafe.LiveChatIntegration;
using MyCafe.Locations;
using MyCafe.CustomerFactory;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;
using Color = Microsoft.Xna.Framework.Color;
using Point = Microsoft.Xna.Framework.Point;
using LogLevel = StreamingClient.Base.Util.LogLevel;
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
#endif
#if YOUTUBE || TWITCH
using StreamingClient.Base.Util;
#endif


// ReSharper disable once CheckNamespace

namespace MyCafe.LiveChatIntegration
{
    internal interface IStreamManager
    {
        public event EventHandler<ChatMessageReceivedEventArgs> OnChatMessageReceived;
        public Task<bool> Connect();
    }

    public class ChatMessageReceivedEventArgs : EventArgs
    {
        public string Username { get; set; }
        public string Message { get; set; }
        public Color Color { get; set; } = Color.White;

    }

    #if TWITCH
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
        private ColorConverter colorConverter = new ColorConverter();

        public async Task<bool> Connect()
        {
            _clientId = ModConfig.LoadedConfig.TwitchClientId;
            _clientSecret = ModConfig.LoadedConfig.TwitchClientSecret;

            try
            {
                Log.Info($"Twitch - Connecting...");
                _connectionState = ConnectionStatus.Connecting;

                List<OAuthClientScopeEnum> scopes =
                [
                    OAuthClientScopeEnum.chat__read,
                    OAuthClientScopeEnum.chat__edit,
                    OAuthClientScopeEnum.whispers__read,
                ];

                _connection = await TwitchConnection.ConnectViaLocalhostOAuthBrowser(_clientId, _clientSecret, scopes, forceApprovalPrompt: false);

                if (_connection != null)
                {
                    Log.Info($"Twitch - Connection successful");
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
            var color = (System.Drawing.Color)(colorConverter.ConvertFromInvariantString(e.Color) ?? System.Drawing.Color.White);
            Color resultColor = new Color(color.R, color.G, color.B);
            OnChatMessageReceived?.Invoke(this, new ChatMessageReceivedEventArgs()
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
    #endif
}

namespace MyCafe.UI
{
    internal class ChatIntegrationPage : OptionsPageBase
    {
        public ChatIntegrationPage(CafeMenu parentMenu, Rectangle bounds) : base("Chat Integration", bounds, parentMenu)
        {
            _options.Add(new OptionsButton("Authorize", ConnectChat)
            {
                style = OptionsElement.Style.Default,
                label = "Connect Stream"
            });
            _options.First().bounds.Width = (int)Game1.dialogueFont.MeasureString("Connect Stream").X + 64;

        }

        private void ConnectChat()
        {
            if (Mod.Cafe.Customers.ChatCustomers is { State: SpawnerState.Disabled })
                Mod.Cafe.Customers.ChatCustomers.Initialize(Mod.ModHelper);
        }
    }
}

namespace MyCafe.CustomerFactory
{
    internal class ChatCustomerSpawner : CustomerSpawner
    {
        private IStreamManager _streamManager;

        private readonly List<string> _users = new List<string>();

        internal override async void Initialize(IModHelper helper)
        {
#if YOUTUBE
            _streamManager = new YoutubeManager();
#elif TWITCH
            _streamManager = new TwitchManager();
#endif

            State = SpawnerState.Initializing;
            if (_streamManager != null && await _streamManager.Connect())
            {
                State = SpawnerState.Enabled;
                _streamManager.OnChatMessageReceived += OnChatMessageReceived;
                Game1.chatBox.addMessage("Live chat connected!", Color.White);
            }
        }

        internal void OnChatMessageReceived(object sender, ChatMessageReceivedEventArgs e)
        {
            Log.Info("Man " + e.Username + ": " + e.Message);
            _users.Add(e.Username);
        }

        internal override bool Spawn(Table table, out CustomerGroup group)
        {
            group = new CustomerGroup();
            if (_users.Count == 0)
                return false;

            string name = _users[Game1.random.Next(_users.Count)];
            Texture2D portrait =
                Game1.content.Load<Texture2D>(Mod.ModHelper.ModContent.GetInternalAssetName(Path.Combine("assets", "Portraits", "cat.png")).Name);
            AnimatedSprite sprite = new AnimatedSprite(Mod.ModHelper.ModContent.GetInternalAssetName(Path.Combine("assets", "Sprites", "customer1.png")).Name,
                0, 16, 32);

            Customer c = new Customer($"ChatCustomerNPC_{name}", new Vector2(10, 12), "BusStop", sprite, portrait)
            {
                portraitOverridden = true,
                displayName = name
            };
            c.DrawName.Set(true);

            group.Add(c);
            group.ReserveTable(table);
            c.ItemToOrder.Set(ItemRegistry.Create<StardewValley.Object>("(O)128"));
            GameLocation busStop = Game1.getLocationFromName("BusStop");
            busStop.addCharacter(c);
            c.Position = new Vector2(33, 9) * 64;

            if (group.MoveToTable() is false)
            {
                Log.Error("Customers couldn't path to cafe");
                LetGo(group, force: true);
                return false;
            }

            Log.Info($"Chat member {name} spawned");
            ActiveGroups.Add(group);
            return true;
        }

        internal override bool LetGo(CustomerGroup group, bool force = false)
        {
            if (!base.LetGo(group))
                return false;

            if (force)
            {
                group.Delete();
            }
            else
            {
                group.MoveTo(
                    Game1.getLocationFromName("BusStop"),
                    new Point(33, 9),
                    (c, loc) => loc.characters.Remove(c as NPC));
            }

            return true;
        }

        internal override void DayUpdate()
        {
            return;
        }
    }
}