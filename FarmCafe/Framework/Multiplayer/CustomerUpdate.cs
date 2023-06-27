using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FarmCafe.Framework.Customers;
using StardewValley;

namespace FarmCafe.Framework.Multiplayer
{
    internal class CustomerUpdate
    {
        // Dictionary - (Customer Name, Location Name)
        public Dictionary<string, string> keyValuePairs;

        public CustomerUpdate() {
            this.keyValuePairs = new Dictionary<string, string>();
        }

        public CustomerUpdate(Customer customer) : this(customer.Name, customer.currentLocation.Name) {}

        public CustomerUpdate(string name, GameLocation location) : this(name, location.Name) {}

        public CustomerUpdate(string name, string locationName) : this()
        {
            this.keyValuePairs.Add(name, locationName);
        }

        public CustomerUpdate(CustomerGroup group)
        {
            this.keyValuePairs = new Dictionary<string, string>();
            foreach (var customer in group.Members)
            {
                this.keyValuePairs.Add(customer.Name, customer.currentLocation.Name);
            }
        }
    }
}
