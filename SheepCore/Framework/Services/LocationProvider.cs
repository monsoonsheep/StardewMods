using StardewValley.Locations;

namespace StardewMods.SheepCore.Framework.Services;
public class LocationProvider
{
    private readonly Dictionary<string, WeakReference<GameLocation>> Cache = [];

    public BusStop BusStop
        => (BusStop)this.Get("BusStop");

    public Farm Farm
        => (Farm)this.Get("Farm");

    public Town Town
        => (Town)this.Get("Town");

    public Beach Beach
        => (Beach)this.Get("Beach");

    public FarmHouse FarmHouse
        => (FarmHouse)this.Get("FarmHouse");

    internal GameLocation Get(string key)
    {
        if (this.Cache.TryGetValue(key, out WeakReference<GameLocation>? cachedRef)
            && cachedRef.TryGetTarget(out GameLocation? loc))
        {
            return loc;
        }

        return this.UpdateLocation(key);
    }

    public GameLocation UpdateLocation(string name)
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
