using StardewValley;
using System.Collections.Generic;

namespace MyCafe.Customers;

public abstract class CustomerData
{
    public int Frequency = 2;
    public List<string> Partners; // will be changed to something more sophisticated soon

    internal WorldDate LastVisitedDate = new(1, Season.Spring, 1);
    internal bool CanVisitToday = false;
}