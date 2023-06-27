using FarmCafe.Framework.Customers;
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
    }
}
