using System;

namespace BCXAPI.Providers
{
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
