using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StardewMods.FoodJoints.Framework.Enums;

namespace StardewMods.FoodJoints.Framework.Data;

internal class TableStateChangedEventArgs : EventArgs
{
    internal TableState OldValue;
    internal TableState NewValue;
}
