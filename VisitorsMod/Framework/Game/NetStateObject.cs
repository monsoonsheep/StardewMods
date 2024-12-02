using System.Xml.Serialization;
using Netcode;
using StardewMods.VisitorsMod.Framework.Data;
using StardewValley.Network;

namespace StardewMods.VisitorsMod.Framework.Game;

[XmlType("Mods_MonsoonSheep_VisitorsMod_VisitorNetState")]
public class NetStateObject : INetObject<NetFields>
{
    public NetFields NetFields { get; } = new NetFields("VisitorNetState");

    public readonly NetStringDictionary<GeneratedSpriteData, NetRef<GeneratedSpriteData>> GeneratedSprites = new();

    public NetStateObject()
    {
        this.NetFields.SetOwner(this).AddField(this.GeneratedSprites, "GeneratedSprites");
        this.GeneratedSprites.OnValueRemoved += (id, data) => data.Dispose();
    }
}
