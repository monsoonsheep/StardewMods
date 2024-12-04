using StardewMods.VisitorsMod.Framework.Services;
using StardewMods.VisitorsMod.Framework.Services.Visitors;

namespace StardewMods.VisitorsMod.Framework;
internal class Debug
{
    internal void Initialize()
    {
        Mod.Events.Input.ButtonPressed += this.OnButtonPressed;
    }

    private void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
    {
        switch (e.Button)
        {
            case SButton.Insert:
                //ModEntry.Visitors.DebugSpawnTestNpc();
                break;
            default:
                break;
        }
    }
}
