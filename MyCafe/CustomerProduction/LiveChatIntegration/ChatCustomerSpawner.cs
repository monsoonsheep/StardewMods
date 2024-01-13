using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MyCafe.ChairsAndTables;
using MyCafe.Customers;
using StardewModdingAPI;
using StardewValley;

namespace MyCafe.CustomerProduction;
internal class ChatCustomerSpawner : ICustomerSpawner
{
    private IStreamManager _streamManager;

    private readonly List<string> _users = new List<string>();

    public void Initialize(IModHelper helper)
    {
        Task.Run(Connect);
    }

    internal async Task Connect()
    {
#if YOUTUBE
        _streamManager = new YoutubeManager();
#elif TWITCH
        _streamManager = new TwitchManager();
#endif

        if (_streamManager != null && await _streamManager.Connect())
        {
            _streamManager.OnChatMessageReceived += OnChatMessageReceived;
            Game1.chatBox.addMessage("Live chat connected!", Color.White);
        }
    }

    internal void OnChatMessageReceived(object sender, ChatMessageReceivedEventArgs e)
    {
        Log.Info("Man " + e.Username + ": " + e.Message);
        _users.Add(e.Username);
    }

    public bool Spawn(Table table, out CustomerGroup group)
    {
        group = new CustomerGroup();
        if (_users.Count == 0)
            return false;

        string name = _users[Game1.random.Next(_users.Count)];
        Texture2D portrait = Game1.content.Load<Texture2D>(Mod.ModHelper.ModContent.GetInternalAssetName(Path.Combine("assets", "Portraits", "cat.png")).Name);
        AnimatedSprite sprite = new AnimatedSprite(Mod.ModHelper.ModContent.GetInternalAssetName(Path.Combine("assets", "Sprites", "customer1.png")).Name, 0, 16, 32);

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
            group.Delete();
            group.ReservedTable.Free();
            return false;
        }

        return true;
    }

    public void LetGo(CustomerGroup group)
    {

    }

    public void DayUpdate()
    {
        return;
    }
}
