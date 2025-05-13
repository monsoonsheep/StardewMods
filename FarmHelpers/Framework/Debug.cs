namespace StardewMods.FarmHelpers.Framework;
internal class Debug
{
    internal static void Initialize()
    {
        Mod.Events.Input.ButtonPressed += OnButtonPressed;
    }

    private static void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
    {
        switch (e.Button)
        {
            case SButton.Insert:
                NPC helper = Game1.getCharacterFromName($"{Mod.Manifest.UniqueID}_Itachi");
                Log.Debug(helper.currentLocation.Name);
                break;
            default:
                break;
        }
    }
}
