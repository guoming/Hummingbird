using System;
using System.Collections.Generic;
using System.Linq;
using CacheManager.Core;

namespace Hummingbird.Extersions.Cache
{
    public class HummingbirdCacheManagerCache<T> : IHummingbirdCache<T>
    {
        private readonly ICacheManager<object> _cacheManager;
        private readonly IHummingbirdCacheConfig _option;

        private string PaddingPrefix(string region)
        {
            return $"{_option.CacheRegion}:{region}";
        }

        public HummingbirdCacheManagerCache(
            ICacheManager<object> cacheManager,
            IHummingbirdCacheConfig option)
        {
            _option = option;
            _cacheManager = cacheManager;
        }

        public void Add(string key, T value, TimeSpan ttl, string region)
        {
            _cacheManager.Add(new CacheItem<object>(key, PaddingPrefix(region), value, ExpirationMode.Absolute, ttl));
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
    }
}