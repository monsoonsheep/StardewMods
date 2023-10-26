using System.Collections.Generic;
using System.Linq;
using FarmCafe.Framework.Characters;
using FarmCafe.Framework.Objects;
using StardewValley;
using StardewValley.Objects;

namespace FarmCafe.Framework.Multiplayer
{
    internal class Sync
    {
        internal static void AddVisitorGroup(VisitorGroup group)
        {
            ModEntry.ModHelper.Multiplayer.SendMessage(group.Members.Select(c => c.Name).ToList(), "UpdateVisitors", modIDs: new[] { ModEntry.ModManifest.UniqueID });
        }

        internal static void RemoveAllVisitors()
        {
            ModEntry.ModHelper.Multiplayer.SendMessage(new List<string>() {}, "RemoveVisitors", modIDs: new[] { ModEntry.ModManifest.UniqueID });
        }

        internal static void UpdateVisitorInfo(Visitor Visitor, string fieldName, object value)
        {
            ModEntry.ModHelper.Multiplayer.SendMessage(value, $"UpdateVisitorInfo/{Visitor.Name}/{fieldName}", modIDs: new[] { ModEntry.ModManifest.UniqueID });

        }

        internal static void SyncTables()
        {
            //Dictionary<Vector2, string> tables = FarmCafe.TableManager.Tables.ToDictionary(x => x.Position, x => x.CurrentLocation.Name);
            //FarmCafe.ModHelper.Sync.SendMessage(tables, "SyncTables", modIDs: new[] { FarmCafe.ModManifest.UniqueID });
        }

        internal static void SendTableClick(Table table, Farmer who)
        {
            ModEntry.ModHelper.Multiplayer.SendMessage(
                message: new Dictionary<string, string>()
                {
                    { "farmer", who.UniqueMultiplayerID.ToString() },
                    { "table", table.Position.ToString() }
                },
                messageType: "ClickTable",
                modIDs: new[] { ModEntry.ModManifest.UniqueID });
        }

        internal static void VisitorDoEmote(Visitor Visitor, int emote)
        {
            ModEntry.ModHelper.Multiplayer.SendMessage(
                message: new Dictionary<string, string>()
                {
                    { "name", Visitor.Name },
                    { "emote", emote.ToString() }
                },
                messageType: "VisitorDoEmote",
                modIDs: new[] { ModEntry.ModManifest.UniqueID });
        }

        internal static void UpdateFurniture(GameLocation location, Furniture furniture)
        {
            ModEntry.ModHelper.Multiplayer.SendMessage(
                message: $"{location.NameOrUniqueName} {furniture.TileLocation.X} {furniture.TileLocation.Y}", 
                messageType: "UpdateFurniture", 
                modIDs: new[] { ModEntry.ModManifest.UniqueID });
        }
    }
}
