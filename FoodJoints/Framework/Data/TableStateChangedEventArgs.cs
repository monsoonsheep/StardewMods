using StardewMods.FoodJoints.Framework.Enums;

namespace StardewMods.FoodJoints.Framework.Data;

internal class TableStateChangedEventArgs : EventArgs
{
    internal TableState OldValue;
    internal TableState NewValue;
}
