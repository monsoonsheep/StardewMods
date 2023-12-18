using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Netcode;
using StardewValley;

namespace StardewCafe.Framework.Customers
{
    internal class Customer : NPC
    {
        internal NetString OrderItem;

        protected override void initNetFields()
        {
            base.initNetFields();
            this.NetFields.AddField(this.OrderItem);
        }


    }
}
