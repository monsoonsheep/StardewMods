using Netcode;
using StardewMods.FoodJoints.Framework.Game;
using StardewMods.FoodJoints.Framework.Inventories;
using StardewMods.FoodJoints.Framework.Objects;

namespace StardewMods.FoodJoints.Framework.Services;
internal class NetState 
{
    internal static NetState Instance = null!;

    internal CafeNetState fields
        => Game1.player.team.get_CafeNetState().Value;

    internal NetCollection<Table> Tables
        => this.fields.Tables;

    internal NetBool CafeEnabled
        => this.fields.CafeEnabled;

    internal NetBool CafeOpen
        => this.fields.CafeOpen;

    internal NetRef<StardewValley.Object?> Signboard
        => this.fields.Signboard;

    internal NetRef<FoodMenuInventory> Menu
        => this.fields.Menu;

    // Fields for syncing data

    internal NetState()
    {
        Instance = this;
    }
}
