namespace StardewMods.BusSchedules.Framework.Services;

/// <summary>
/// The scheduled departure and arrival times of the bus
/// </summary>
public class Timings
{
    internal static Timings Instance = null!;

    internal byte BusArrivalsToday;

    internal int NextArrivalTime
        => this.BusArrivalsToday < Values.BusArrivalTimes.Length ? Values.BusArrivalTimes[this.BusArrivalsToday] : 99999;

    internal int LastArrivalTime
        => this.BusArrivalsToday == 0 ? 0 : Values.BusArrivalTimes[this.BusArrivalsToday - 1];

    internal int TimeUntilNextArrival
        => Utility.CalculateMinutesBetweenTimes(Game1.timeOfDay, this.NextArrivalTime);

    internal int TimeSinceLastArrival
        => Utility.CalculateMinutesBetweenTimes(this.LastArrivalTime, Game1.timeOfDay);

    internal Timings()
        => Instance = this;

    internal bool CheckLeaveTime(int time)
    {
        if (this.BusArrivalsToday == 0)
        {
            return Utility.CalculateMinutesBetweenTimes(time, this.NextArrivalTime) == 20;
        }
        else if (this.BusArrivalsToday == 1)
        {
            return Utility.CalculateMinutesBetweenTimes(time, this.NextArrivalTime) == 70;
        }
        else if (Utility.CalculateMinutesBetweenTimes(this.LastArrivalTime, time) == 20)
        {
            return true;
        }

        return false;
    }

    internal bool CheckReturnTime(int time)
    {
        if (Utility.CalculateMinutesBetweenTimes(time, this.NextArrivalTime) == 10)
        {
            return true;
        }

        return false;
    }
}
