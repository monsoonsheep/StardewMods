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
        internal static void AddCustomerGroup(CustomerGroup group)
        {
            ModEntry.ModHelper.Multiplayer.SendMessage(group.Members.Select(c => c.Name).ToList(), "UpdateCustomers", modIDs: new[] { ModEntry.ModManifest.UniqueID });
        }

        internal static void RemoveAllCustomers()
        {
            ModEntry.ModHelper.Multiplayer.SendMessage(new List<string>() {}, "RemoveCustomers", modIDs: new[] { ModEntry.ModManifest.UniqueID });
        }

        internal static void UpdateCustomerInfo(Customer customer, string fieldName, object value)
        {
            ModEntry.ModHelper.Multiplayer.SendMessage(value, $"UpdateCustomerInfo/{customer.Name}/{fieldName}", modIDs: new[] { ModEntry.ModManifest.UniqueID });

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

        internal static void CustomerDoEmote(Customer customer, int emote)
        {
            ModEntry.ModHelper.Multiplayer.SendMessage(
                message: new Dictionary<string, string>()
                {
                    { "name", customer.Name },
                    { "emote", emote.ToString() }
                },
                messageType: "CustomerDoEmote",
                modIDs: new[] { ModEntry.ModManifest.UniqueID });
        }

        internal static void UpdateFurniture(GameLocation location, Furniture furniture)
        {
            ModEntry.ModHelper.Multiplayer.SendMessage(
                message: $"{location.NameOrUniqueName} {furniture.TileLocation.X} {furniture.TileLocation.Y}", 
                messageType: "UpdateFurniture", 
                modIDs: new[] { ModEntry.ModManifest.UniqueID });
        }

        internal static List<Customer> GetAllCustomersInGame()
        {
            var locationCustomers = Game1.locations
                .SelectMany(l => l.getCharacters())
                .OfType<Customer>();

            var buildingCustomers = (Game1.getFarm().buildings
                    .Where(b => b.indoors.Value != null)
                    .SelectMany(b => b.indoors.Value.characters))
                .OfType<Customer>();

            var list = locationCustomers.Concat(buildingCustomers).ToList();

            Logger.Log("Updating customers" + string.Join(' ', list));
            return list;
        }
    }
}
