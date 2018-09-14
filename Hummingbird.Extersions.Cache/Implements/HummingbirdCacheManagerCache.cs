using System;
using System.Collections.Generic;
using System.Linq;
using CacheManager.Core;

namespace Hummingbird.Extersions.Cache
{
    public class HummingbirdCacheManagerCache<T> : IHummingbirdCache<T>
    {
        private readonly ICacheManager<T> _cacheManager;
        private readonly string _CacheRegion;

        private string PaddingPrefix(string region)
        {
            return $"{_CacheRegion}:{region}";
        }

        public HummingbirdCacheManagerCache(
            ICacheManager<T> cacheManager,
            string CacheRegion)
        {
            _CacheRegion = CacheRegion;
            _cacheManager = cacheManager;
        }

        public void Add(string key, T value, TimeSpan ttl, string region)
        {
            _cacheManager.Put(new CacheItem<T>(key, PaddingPrefix(region), value, ExpirationMode.Absolute, ttl));
        }

        public bool Exists(string key, string region)
        {
            return key!=null && _cacheManager.Exists(key, PaddingPrefix(region));
        }

        public T Get(string key, string region)
        {
            return _cacheManager.Get<T>(key, PaddingPrefix(region));
        }

        public void ClearRegion(string region)
        {
            _cacheManager.ClearRegion(PaddingPrefix(region));
        }

        public bool Delete(string key, string region)
        {
            return _cacheManager.Remove(key, PaddingPrefix(region));
        }
    }
}