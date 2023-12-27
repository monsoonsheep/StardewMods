using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyCafe.Framework.Customers
{
    internal class CustomerGroup
    {
        internal List<Customer> Members;

        internal CustomerGroup(List<Customer> members)
        {
            Members = members;
        }
    }
}
