using FarmCafe.Framework.Characters;
using FarmCafe.Framework.Managers;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Objects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FarmCafe.Framework.Objects;
using StardewValley.Menus;
using static FarmCafe.Framework.Utilities.Utility;

namespace FarmCafe.Framework
{
    internal static class Multiplayer
    {
        internal static void AddCustomerGroup(CustomerGroup group)
        {
            FarmCafe.ModHelper.Multiplayer.SendMessage(group.Members.Select(c => c.Name).ToList(), "UpdateCustomers", modIDs: new[] { FarmCafe.ModManifest.UniqueID });
        }

        internal static void RemoveAllCustomers()
        {
            FarmCafe.ModHelper.Multiplayer.SendMessage(new List<string>() {}, "RemoveCustomers", modIDs: new[] { FarmCafe.ModManifest.UniqueID });
        }

        internal static void UpdateCustomerInfo(Customer customer, string fieldName, object value)
        {
            FarmCafe.ModHelper.Multiplayer.SendMessage(value, $"UpdateCustomerInfo/{customer.Name}/{fieldName}", modIDs: new[] { FarmCafe.ModManifest.UniqueID });

        }

        internal static void SyncTables()
        {
            //Dictionary<Vector2, string> tables = FarmCafe.TableManager.Tables.ToDictionary(x => x.Position, x => x.CurrentLocation.Name);
            //FarmCafe.ModHelper.Multiplayer.SendMessage(tables, "SyncTables", modIDs: new[] { FarmCafe.ModManifest.UniqueID });
        }

        internal static void SendTableClick(ITable table, Farmer who)
        {
            FarmCafe.ModHelper.Multiplayer.SendMessage(
                message: new Dictionary<string, string>()
                {
                    { "farmer", who.UniqueMultiplayerID.ToString() },
                    { "table", table.Position.ToString() }
                },
                messageType: "ClickTable",
                modIDs: new[] { FarmCafe.ModManifest.UniqueID });
        }

        internal static void CustomerDoEmote(Customer customer, int emote)
        {
            FarmCafe.ModHelper.Multiplayer.SendMessage(
                message: new Dictionary<string, string>()
                {
                    { "name", customer.Name },
                    { "emote", emote.ToString() }
                },
                messageType: "CustomerDoEmote",
                modIDs: new[] { FarmCafe.ModManifest.UniqueID });
        }

        internal static void UpdateFurniture(GameLocation location, Furniture furniture)
        {
            FarmCafe.ModHelper.Multiplayer.SendMessage(
                message: $"{location.NameOrUniqueName} {furniture.TileLocation.X} {furniture.TileLocation.Y}", 
                messageType: "UpdateFurniture", 
                modIDs: new[] { FarmCafe.ModManifest.UniqueID });
        }
    }
}
