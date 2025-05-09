using StardewMods.FoodJoints.Framework.UI;

namespace StardewMods.FoodJoints.Framework.Services;
internal class LocationManager
{
    internal static LocationManager Instance = null!;

    internal StardewValley.Object? Signboard
    {
        get => Mod.NetState.Signboard.Value;
        set => Mod.NetState.Signboard.Set(value);
    }

    internal LocationManager()
    {
        Instance = this;

        GameLocation.RegisterTileAction(Values.SIGNBOARD_BUILDING_CLICK_EVENT_KEY, CafeMenu.Action_OpenCafeMenu);
    }

    internal bool UpdateSignboard()
    {
        return (this.Signboard = this.GetSignboardObject()) != null;
    }

    internal void OnPlacedDownSignboard(StardewValley.Object signboard)
    {
        if (this.Signboard != null)
        {
            Log.Error($"There is already a signboard registered in {this.Signboard.Location.DisplayName}");
            return;
        }

        if (!Mod.Cafe.Open)
        {
            Mod.Cafe.UpdateLocations();
        }
    }

    internal void OnRemovedSignboard(StardewValley.Object signboard)
    {
        if (this.Signboard != null)
        {
            if (Mod.Cafe.Open)
            {
                Log.Warn("Player broke the signboard while the cafe was open");
            }

            Game1.delayedActions.Add(new DelayedAction(500, () =>
            {
                Mod.Cafe.UpdateLocations();
                Mod.Customers.RemoveAllCustomers();
            }));
        }
    }

    internal StardewValley.Object? GetSignboardObject()
    {
        StardewValley.Object? found = null;

        Utility.ForEachLocation(delegate (GameLocation loc)
        {
            foreach (StardewValley.Object obj in loc.Objects.Values)
            {
                if (obj.QualifiedItemId.Equals($"(BC){Values.CAFE_SIGNBOARD_OBJECT_ID}"))
                {
                    found = obj;
                    return false;
                }
            }

            return true;
        });

        return found;
    }

    internal void GiveSignboard(string command, string[] args)
    {
        if (Game1.player.Items.ContainsId(Values.CAFE_SIGNBOARD_OBJECT_ID))
        {
            Log.Warn("You already have the signboard in your inventory");
            return;
        }

        StardewValley.Object signboard = ItemRegistry.Create<StardewValley.Object>($"(BC){Values.CAFE_SIGNBOARD_OBJECT_ID}");
        Game1.player.addItemToInventory(signboard);
    }
}
