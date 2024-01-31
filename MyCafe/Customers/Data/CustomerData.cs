using StardewValley;

namespace MyCafe.Customers.Data;

public abstract class CustomerData
{
    public int Frequency = 2;
    internal WorldDate LastVisitedDate = new(1, Season.Spring, 1);
    internal bool CanVisitToday = false;
}