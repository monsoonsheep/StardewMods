using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using MyCafe.Characters.Spawning;
using MyCafe.Data.Customers;
using MyCafe.Inventories;
using MyCafe.Locations.Objects;
using Netcode;
using StardewValley.Network;

namespace MyCafe.Netcode;

[XmlType( "Mods_MonsoonSheep_MyCafe_CafeNetFields" )]
public class CafeNetFields : INetObject<NetFields>
{
    public NetFields NetFields { get; } = new NetFields("CafeNetFields");

    public readonly NetCollection<Table> NetTables = [];
    public readonly NetBool CafeEnabled = [];
    public readonly NetLocationRef CafeIndoor = new();
    public readonly NetLocationRef CafeOutdoor = new();

    public readonly NetInt OpeningTime = new(630);
    public readonly NetInt ClosingTime = new(2200);
    public readonly NetRef<MenuInventory> NetMenu = new(new MenuInventory());
    public readonly NetStringHashSet NpcCustomers = new NetStringHashSet();

    public readonly NetStringDictionary<GeneratedSpriteData, NetRef<GeneratedSpriteData>> GeneratedSprites = new();

    public CafeNetFields()
    {
        this.NetFields.SetOwner(this)
            .AddField(this.OpeningTime, "OpeningTime").AddField(this.ClosingTime, "ClosingTime").AddField(this.NetTables, "NetTables").AddField(this.CafeEnabled, "CafeEnabled").AddField(this.CafeIndoor.NetFields, "CafeIndoor.NetFields")
            .AddField(this.CafeOutdoor.NetFields, "CafeOutdoor.NetFields").AddField(this.NetMenu, "NetMenu").AddField(this.NpcCustomers).AddField(this.GeneratedSprites, "GeneratedSprites");
    }
}
