using System.Xml.Serialization;
using Netcode;
using StardewMods.MyShops.Framework.Inventories;
using StardewMods.MyShops.Framework.Objects;
using StardewValley.Network;

#nullable disable

namespace StardewMods.MyShops.Framework.Game;

[XmlType("Mods_MonsoonSheep_MyShops_NetStateObject")]
public class NetStateObject : INetObject<NetFields>
{
    public NetFields NetFields { get; } = new NetFields("NetStateObject");

    public readonly NetCollection<Table> Tables = [];
    public readonly NetBool CafeEnabled = new(false);
    public readonly NetBool CafeOpen = new(false);
    public readonly NetRef<StardewValley.Object> Signboard = new(null);
    public readonly NetRef<FoodMenuInventory> Menu = new(new FoodMenuInventory());

    public NetStateObject()
    {
        this.NetFields.SetOwner(this).AddField(this.Tables, "Tables").AddField(this.CafeEnabled, "CafeEnabled")
            .AddField(this.CafeOpen, "CafeOpen").AddField(this.Signboard, "Signboard").AddField(this.Menu, "Menu");
    }
}
