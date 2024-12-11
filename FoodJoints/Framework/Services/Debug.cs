using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StardewMods.FoodJoints.Framework.Services;
internal class Debug
{
    internal static Debug Instance = null!;

    internal Debug()
        => Instance = this;

    internal void Initialize()
    {   
        Mod.Events.Input.ButtonPressed += this.OnButtonPressed;
    }

    private void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
    {
        switch (e.Button)
        {
            case SButton.Insert:
                Mod.Customers.SpawnVillagerCustomers();
                break;
            default:
                break;
        }
    }
}
