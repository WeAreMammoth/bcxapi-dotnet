using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BCXAPI.Providers
{
    /// <summary>
    /// implement this interface and provide your implementation to the BCXAPI.Service constructor to use it as 
    /// to cache responses from Basecamp.
    /// </summary>
    public interface IResponseCache
    {
        /// <summary>
        /// get an object from the cache
        /// </summary>
        /// <param name="key">key of the item</param>
        /// <returns></returns>
         object Get(string key);
        /// <summary>
        /// set the object in the cache - overwrite existing key if it exists
        /// </summary>
        /// <param name="key"></param>
        /// <param name="store_me"></param>
        /// <returns></returns>
         bool Set(string key, object store_me);

        /// <summary>
        /// remove the object from the cache and return it
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
         object Remove(string key);

    }
}
