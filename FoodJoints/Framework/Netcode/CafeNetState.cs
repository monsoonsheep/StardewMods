using System.Xml.Serialization;
using Netcode;
using StardewMods.FoodJoints.Framework.Data;
using StardewMods.FoodJoints.Framework.Inventories;
using StardewMods.FoodJoints.Framework.Objects;
using StardewValley.Network;

#nullable disable

namespace StardewMods.FoodJoints.Framework.Game;

[XmlType("Mods_MonsoonSheep_FoodJoints_CafeNetState")]
public class CafeNetState : INetObject<NetFields>
{
    public NetFields NetFields { get; } = new NetFields("CafeNetState");

    public readonly NetCollection<Table> Tables = [];
    public readonly NetBool CafeEnabled = new(false);
    public readonly NetBool CafeOpen = new(false);
    public readonly NetRef<StardewValley.Object> Signboard = new(null);
    public readonly NetRef<FoodMenuInventory> Menu = new(new FoodMenuInventory());

    public CafeNetState()
    {
        this.NetFields.SetOwner(this).AddField(this.Tables, "Tables").AddField(this.CafeEnabled, "CafeEnabled")
            .AddField(this.CafeOpen, "CafeOpen").AddField(this.Signboard, "Signboard").AddField(this.Menu, "Menu");

        this.Tables.OnValueAdded += table =>
            table.State.fieldChangeVisibleEvent += (_, oldValue, newValue) => Mod.Tables.OnTableStateChange(table, new TableStateChangedEventArgs()
            {
                OldValue = oldValue,
                NewValue = newValue
            });
    }
}
