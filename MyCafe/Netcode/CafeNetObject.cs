using System.Xml.Serialization;
using MyCafe.Data.Customers;
using MyCafe.Inventories;
using MyCafe.Locations.Objects;
using Netcode;
using StardewValley;
using StardewValley.Network;

#nullable disable

namespace MyCafe.Netcode;

[XmlType( "Mods_MonsoonSheep_MyCafe_CafeNetFields" )]
public class CafeNetObject : INetObject<NetFields>
{
    public NetFields NetFields { get; } = new NetFields("CafeNetFields");

    public readonly NetCollection<Table> NetTables = [];
    public readonly NetBool CafeEnabled = [];
    public readonly NetLocationRef BuildingInterior = new(null);
    public readonly NetRef<Object> Signboard = new(null);

    public readonly NetInt OpeningTime = new(630);
    public readonly NetInt ClosingTime = new(2200);
    public readonly NetRef<FoodMenuInventory> NetMenu = new(new FoodMenuInventory());
    public readonly NetStringHashSet NpcCustomers = new NetStringHashSet();

    public readonly NetStringDictionary<GeneratedSpriteData, NetRef<GeneratedSpriteData>> GeneratedSprites = new();

    public CafeNetObject()
    {
        this.NetFields.SetOwner(this)
            .AddField(this.OpeningTime, "OpeningTime").AddField(this.ClosingTime, "ClosingTime").AddField(this.NetTables, "NetTables").AddField(this.CafeEnabled, "CafeEnabled").AddField(this.BuildingInterior.NetFields, "BuildingInterior.NetFields")
            .AddField(this.Signboard, "Signboard").AddField(this.NetMenu, "NetMenu").AddField(this.NpcCustomers).AddField(this.GeneratedSprites, "GeneratedSprites");
    }
}
