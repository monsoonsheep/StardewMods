using System.Xml.Serialization;
using Monsoonsheep.StardewMods.MyCafe.Data.Customers;
using Monsoonsheep.StardewMods.MyCafe.Inventories;
using Monsoonsheep.StardewMods.MyCafe.Locations.Objects;
using Netcode;
using StardewValley;
using StardewValley.Network;

#nullable disable

namespace Monsoonsheep.StardewMods.MyCafe.Netcode;

[XmlType( "Mods_MonsoonSheep_MyCafe_CafeNetFields" )]
public class CafeNetObject : INetObject<NetFields>
{
    public NetFields NetFields { get; } = new NetFields("CafeNetFields");

    public readonly NetCollection<Table> NetTables = [];
    public readonly NetByte CafeEnabled = [];
    public readonly NetRef<Object> Signboard = new(null);

    public readonly NetRef<FoodMenuInventory> NetMenu = new(new FoodMenuInventory());
    public readonly NetStringHashSet NpcCustomers = new NetStringHashSet();

    public readonly NetStringDictionary<GeneratedSpriteData, NetRef<GeneratedSpriteData>> GeneratedSprites = new();

    public CafeNetObject()
    {
        this.NetFields.SetOwner(this)
            .AddField(this.NetTables, "NetTables").AddField(this.CafeEnabled, "CafeEnabled")
            .AddField(this.Signboard, "Signboard").AddField(this.NetMenu, "NetMenu").AddField(this.NpcCustomers).AddField(this.GeneratedSprites, "GeneratedSprites");
    }
}
