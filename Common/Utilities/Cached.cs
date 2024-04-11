using System;

namespace MonsoonSheep.Stardew.Common.Utilities
{
    /// <summary>Maintains a cached value which is updated automatically when the cache key changes.</summary>
    internal class Cached<TKey, TValue>
    {
        /// <summary>Get the current cache key.</summary>
        private readonly Func<TKey> GetCacheKey;

        /// <summary>Fetch the latest value for the cache.</summary>
        private readonly Func<TKey, TValue> FetchNew;

        /// <summary>The last cache key which was cached.</summary>
        private TKey? LastKey;

        /// <summary>The cached value.</summary>
        private TValue? LastValue;

        private bool Valid;

        /// <summary>Construct an instance.</summary>
        /// <param name="getCacheKey">Get the current cache key.</param>
        /// <param name="fetchNew">Fetch the latest value for the cache.</param>
        public Cached(Func<TKey> getCacheKey, Func<TKey, TValue> fetchNew)
        {
            this.GetCacheKey = getCacheKey;
            this.FetchNew = fetchNew;
        }

        public void Invalidate()
        {
            this.Valid = false;
        }

        /// <summary>Get the cached value, creating it if needed.</summary>
        public TValue Value
        {
            get
            {
                TKey key = this.GetCacheKey();
                if (!this.Valid || (key == null ? this.LastKey != null : !key.Equals(this.LastKey)))
                {
                    this.LastKey = key;
                    this.LastValue = this.FetchNew(key);
                    this.Valid = true;
                }

                return this.LastValue!;
            }
        }
    }
}
