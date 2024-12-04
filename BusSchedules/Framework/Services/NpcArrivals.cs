using StardewModdingAPI.Events;
using StardewMods.BusSchedules.Framework.Data;

namespace StardewMods.BusSchedules.Framework.Services;
public class NpcArrivals
{
    internal static NpcArrivals Instance = null!;

    // Dependencies
    private Timings busTimings = null!;

    // State
    private PriorityQueue<NPC, int> busVisitorsQueue = new();

    internal NpcArrivals()
        => Instance = this;

    internal void Initialize()
    {
        this.busTimings = Mod.Instance.BusManager.Timings;

        Mod.Instance.Events.GameLoop.TimeChanged += this.OnTimeChanged;
        Mod.Instance.ModEvents.BusArrive += this.OnBusArrive;
    }

    internal void AddVisitor(NPC npc)
    {
        this.busVisitorsQueue.Enqueue(npc, 0);
    }

    private void OnTimeChanged(object? sender, TimeChangedEventArgs e)
    {
        if (Utility.CalculateMinutesBetweenTimes(e.NewTime, this.busTimings.NextArrivalTime) == 10)
        {
            foreach (var pair in Mod.Instance.VisitorsData)
            {
                NPC npc = Game1.getCharacterFromName(pair.Key);
                if (npc?.ScheduleKey != null
                    && pair.Value.BusVisitSchedules.TryGetValue(npc.ScheduleKey, out var arrivalDepartureIndices)
                    && arrivalDepartureIndices.Item1 == this.busTimings.NextArrivalTime
                    && !this.busVisitorsQueue.UnorderedItems.Any(n => ReferenceEquals(n.Element, npc)))
                {
                    this.busVisitorsQueue.Enqueue(npc, 0);
                }
            }
        }
    }

    /// <summary>
    /// Spawn queued up visitors on the bus door 
    /// </summary>
    private void OnBusArrive(object? sender, BusArriveEventArgs args)
    {
        int count = 0;
        while (this.busVisitorsQueue.TryDequeue(out NPC? visitor, out int priority))
        {
            Game1.delayedActions.Add(new DelayedAction(count * 800 + Game1.random.Next(0, 100), delegate
            {
                visitor.Position = Values.BusDoorTile.ToVector2() * 64f;

                if (visitor.IsReturningToEndPoint())
                {
                    // TODO Is this needed anymore?
                    AccessTools.Field(typeof(NPC), "returningToEndPoint").SetValue(visitor, false);
                    AccessTools.Field(typeof(Character), "freezeMotion").SetValue(visitor, false);
                }
                else
                {
                    visitor.checkSchedule(this.busTimings.LastArrivalTime);
                }

                Log.Debug($"Visitor {visitor.displayName} arrived at {Game1.timeOfDay}");
            }));
            count++;
        }
    }
}
