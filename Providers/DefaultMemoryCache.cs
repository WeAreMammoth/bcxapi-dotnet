using System;

namespace BCXAPI.Providers
{
    /// <summary>
    /// if you do not provide your own implmentation of the IResponseCache interface,
    /// this is used to cache responses from Basecamp via the System.Runtime.Caching.MemoryCache.Default cache.
    /// </summary>
    /// <seealso cref="BCXAPI.Providers.IResponseCache"/>
    public class DefaultMemoryCache : BCXAPI.Providers.IResponseCache
    {
        public object Get(string key)
        {
            return (object)System.Runtime.Caching.MemoryCache.Default.Get(key);
        }

        public bool Set(string key, object store_me)
        {
            System.Runtime.Caching.MemoryCache.Default.Set(key, store_me, new System.Runtime.Caching.CacheItemPolicy());
            return true;
        }

        public object Remove(string key)
        {
           return (object)System.Runtime.Caching.MemoryCache.Default.Remove(key);
        }
    }
}
