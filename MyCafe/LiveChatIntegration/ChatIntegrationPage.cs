using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MyCafe.CustomerFactory;
using MyCafe.UI;
using MyCafe.UI.Options;
using StardewValley;
using StardewValley.Menus;
// ReSharper disable once CheckNamespace

namespace MyCafe.UI;
internal class ChatIntegrationPage : OptionsPageBase
{
    public ChatIntegrationPage(CafeMenu parentMenu) : base("Chat Integration", parentMenu)
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
