using StardewValley;

namespace MyCafe.Data.Customers;

public abstract class CustomerData
{
    public int Frequency = 2;
    internal WorldDate LastVisitedDate = new(1, Season.Spring, 1);
    internal bool CanVisitToday = false;
}