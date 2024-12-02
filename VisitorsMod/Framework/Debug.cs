using StardewMods.VisitorsMod.Framework.Services;
using StardewMods.VisitorsMod.Framework.Services.Visitors;
using StardewMods.VisitorsMod.Framework.Services.Visitors.Activities;

namespace StardewMods.VisitorsMod.Framework;
internal class Debug : Service
{
    private readonly IModHelper modHelper;

    private readonly VisitorManager visitors;
    private readonly NetState netState;
    private readonly ActivityManager activities;
    public Debug(
        VisitorManager visitors,
        NetState netState,
        ActivityManager activities,
        IModHelper modHelper,
        IModEvents events,
        ILogger logger,
        IManifest manifest)
        : base(logger, manifest)
    {
        this.visitors = visitors;
        this.netState = netState;
        this.activities = activities;

        this.modHelper = modHelper;

        events.Input.ButtonPressed += this.OnButtonPressed;
    }

    private void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
    {
        switch (e.Button)
        {
            case SButton.Insert:
                this.visitors.DebugSpawnTestNpc();
                break;
            default:
                break;
        }
    }
}
