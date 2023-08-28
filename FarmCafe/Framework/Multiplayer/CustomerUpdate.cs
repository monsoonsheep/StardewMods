using System.Collections.Generic;
using FarmCafe.Framework.Characters;
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
                    Logger.Log("Can't get character)");
                }
                this.names.Add(customer.Name);
            }
        }
    }
}
