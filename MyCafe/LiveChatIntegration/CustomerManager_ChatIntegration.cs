using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StardewModdingAPI;

namespace MyCafe;
internal partial class CustomerManager
{
    internal partial void GetLiveChatIntegration(IModHelper helper)
    {
        ChatCustomers = new ChatCustomerSpawner();
        ChatCustomers.Initialize(helper);
    }
}
