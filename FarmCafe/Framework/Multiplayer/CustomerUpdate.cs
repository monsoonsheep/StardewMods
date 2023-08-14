using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FarmCafe.Framework.Characters;
using StardewValley;

namespace FarmCafe.Framework
{
    internal class CustomerUpdate
    {
        // Dictionary - (Customer Name, Location Name)
        public List<string> names;

        public CustomerUpdate() {
            this.names = new List<string>();
        }

        public CustomerUpdate(string name)
        {
            this.names = new List<string>() { name };
        }

        public CustomerUpdate(CustomerGroup group)
        {
            this.names = new List<string>();
            foreach (var customer in group.Members)
            {
                if ((Game1.getCharacterFromName(customer.Name) as Customer) == null)
                {
                    Debug.Log("Can't get character)");
                }
                this.names.Add(customer.Name);
            }
        }
    }
}
