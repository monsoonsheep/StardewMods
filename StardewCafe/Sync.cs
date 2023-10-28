using System.Collections.Generic;
using System.Linq;
using StardewCafe.Framework.Objects;
using VisitorFramework.Framework.Characters;
using StardewValley;
using StardewValley.Objects;

namespace StardewCafe
{
    internal class Sync
    {
        internal static void SyncTables()
        {
            //Dictionary<Vector2, string> tables = VisitorFramework.TableManager.Tables.ToDictionary(x => x.Position, x => x.CurrentLocation.Name);
            //VisitorFramework.ModHelper.Sync.SendMessage(tables, "SyncTables", modIDs: new[] { VisitorFramework.ModManifest.UniqueID });
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

        internal static void UpdateFurniture(GameLocation location, Furniture furniture)
        {
            ModEntry.ModHelper.Multiplayer.SendMessage(
                message: $"{location.NameOrUniqueName} {furniture.TileLocation.X} {furniture.TileLocation.Y}", 
                messageType: "UpdateFurniture", 
                modIDs: new[] { ModEntry.ModManifest.UniqueID });
        }
    }
}
