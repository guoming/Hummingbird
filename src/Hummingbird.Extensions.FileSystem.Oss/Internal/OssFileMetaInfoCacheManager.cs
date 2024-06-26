using System;
using Microsoft.Extensions.Caching.Memory;


namespace Hummingbird.Extensions.FileSystem.Oss
{
    public class OssFileMetaInfoCacheManager
    {
        private readonly Config _config;

        public OssFileMetaInfoCacheManager(Config config)
        {
            this._config = config;
        }

        private static IMemoryCache memoryCache = new MemoryCache(new MemoryCacheOptions()
        { 
  
        });

        public TimeSpan GetCacheAbsoluteExpiration()
        {
            return TimeSpan.FromSeconds(_config.CacheOssFileMetaAbsoluteExpirationSeconds);

        }
        
        public IMemoryCache getMemoryCache()
        {
            return memoryCache;
        }
        
    }
}