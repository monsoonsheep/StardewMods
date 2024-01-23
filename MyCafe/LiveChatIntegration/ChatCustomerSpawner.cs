using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MyCafe.CustomerFactory;
using MyCafe.Customers;
using MyCafe.LiveChatIntegration;
using MyCafe.Locations;
using StardewModdingAPI;
using StardewValley;
// ReSharper disable once CheckNamespace

namespace MyCafe.CustomerFactory;
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
        c.ItemToOrder.Set(ItemRegistry.Create<Object>("(O)128"));
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
