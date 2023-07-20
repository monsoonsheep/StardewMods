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
        public List<string> names;

        public CustomerUpdate() {
            this.names = new List<string>();
        }

        public CustomerUpdate(Customer customer) : this(customer.Name) {}

        public CustomerUpdate(string name)
        {
            this.names = new List<string>() { name };
        }

        public CustomerUpdate(CustomerGroup group)
        {
            this.names = new List<string>();
            foreach (var customer in group.Members)
            {
                this.names.Add(customer.Name);
            }
        }
    }
}
