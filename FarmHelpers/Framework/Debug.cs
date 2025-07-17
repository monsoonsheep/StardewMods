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
                Utility.ForEachBuilding((b) =>
                {
                    if (b.buildingType.Value == "Coop")
                    {
                        Log.Debug($"{Game1.player.TilePoint}");
                        
                   

                    }
                    return true;
                });
                break;
            default:
                break;
        }
    }
}
