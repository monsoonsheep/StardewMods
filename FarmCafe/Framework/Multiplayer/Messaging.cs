using FarmCafe.Framework.Customers;
using FarmCafe.Framework.Managers;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Objects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FarmCafe.Framework.Multiplayer
{
    internal static class Messaging
    {
        internal static void AddCustomerGroup(CustomerGroup group)
        {
            CustomerUpdate message = new CustomerUpdate(group);
            FarmCafe.ModHelper.Multiplayer.SendMessage(message, "UpdateCustomers", modIDs: new[] { FarmCafe.ModManifest.UniqueID });
        }

        internal static void RemoveAllCustomers()
        {
            CustomerUpdate message = new CustomerUpdate();
            FarmCafe.ModHelper.Multiplayer.SendMessage(message, "UpdateCustomers", modIDs: new[] { FarmCafe.ModManifest.UniqueID });
        }

        internal static void SyncTables()
        {
            List<Vector2> positions = TableManager.TrackedTables.Select((table) => table.TileLocation).ToList();
            FarmCafe.ModHelper.Multiplayer.SendMessage(positions, "SyncTables", modIDs: new[] { FarmCafe.ModManifest.UniqueID });
        }
    }
}
