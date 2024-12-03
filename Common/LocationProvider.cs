using StardewValley.Locations;

namespace MonsoonSheep.Stardew.Common;
internal class LocationProvider : Service
{
    private readonly Dictionary<string, WeakReference<GameLocation>> Cache = [];

    internal BusStop BusStop
        => (BusStop)this.Get("BusStop");

    internal Farm Farm
        => (Farm)this.Get("Farm");

    internal Town Town
        => (Town)this.Get("Town");

    internal Beach Beach
        => (Beach)this.Get("Beach");

    internal FarmHouse FarmHouse
        => (FarmHouse)this.Get("FarmHouse");

    public LocationProvider(
        ILogger logger,
        IManifest manifest)
        : base(logger, manifest)
    {
    }

    internal GameLocation Get(string key)
    {
        if (this.Cache.TryGetValue(key, out WeakReference<GameLocation>? cachedRef)
            && cachedRef.TryGetTarget(out GameLocation? loc))
        {
            return loc;
        }

        return this.UpdateLocation(key);
    }

    internal GameLocation UpdateLocation(string name)
    {
        GameLocation result = Game1.getLocationFromName(name);

        if (this.Cache.ContainsKey(name))
        {
            this.Cache[name].SetTarget(result);
        }
        else
        {
            this.Cache[name] = new WeakReference<GameLocation>(result);
        }

        return result;
    }
}
