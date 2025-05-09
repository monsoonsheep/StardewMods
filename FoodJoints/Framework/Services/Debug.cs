using StardewValley.Objects;

namespace StardewMods.FoodJoints.Framework.Services;
internal class Debug
{
    internal static Debug Instance = null!;

    internal Debug()
    {
        Instance = this;

        Mod.Events.Input.ButtonPressed += this.OnButtonPressed;
    }

    private void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
    {
        switch (e.Button)
        {
            case SButton.Insert:
                Mod.Customers.SpawnCustomers(Enums.CustomerGroupType.Villager);
                break;
            case SButton.End:
                Game1.player.addItemsByMenuIfNecessary([new Furniture("6", Vector2.Zero), new Furniture("6", Vector2.Zero), new Furniture("1220", Vector2.Zero)]);
                break;
            case SButton.Delete:
                Game1.player.addItemsByMenuIfNecessary([ItemRegistry.Create($"(BC){Values.CAFE_SIGNBOARD_OBJECT_ID}", 1)]);
                break;
            default:
                break;
        }
    }
}
