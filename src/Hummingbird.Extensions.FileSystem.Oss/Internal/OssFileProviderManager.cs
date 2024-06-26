using System.Collections.Concurrent;
using Hummingbird.Extensions.FileSystem.Oss;
using Microsoft.Extensions.FileProviders;


namespace Hummingbird.Extensions.FileSystem
{
    public class OssFileProviderManager
    {
        private static object _syncRoot = new object();
        private readonly PhysicalFileProvider _physicalFileProvider;
        private readonly Config _config;
        private readonly OssFileMetaInfoCacheManager _ossFileMetaInfoCacheManager;
        private readonly ConcurrentDictionary<string, IFileProvider> _fileProviders = new ConcurrentDictionary<string, IFileProvider>();

        public OssFileProviderManager(
            PhysicalFileProvider physicalFileProvider,
            Config config,
            OssFileMetaInfoCacheManager ossFileMetaInfoCacheManager)
        {
            this._physicalFileProvider = physicalFileProvider;
            this._config = config;
            this._ossFileMetaInfoCacheManager = ossFileMetaInfoCacheManager;

            foreach (var item in config.Endpoints)
            {
                GetFileProvider(item.Key);
            }
        }

        public IFileProvider GetFileProvider(string upstream)
        {
            if (_fileProviders.ContainsKey(upstream))
            {
                return _fileProviders[upstream];
            }

            if (!_fileProviders.ContainsKey(upstream))
            {
                lock (_syncRoot)
                {
                    if (!_fileProviders.ContainsKey(upstream))
                    {
                        var client = new OssFileProvider(
                            _physicalFileProvider,
                            _config,
                            _ossFileMetaInfoCacheManager,
                            upstream);     
                        
                        _fileProviders[upstream] = client;
                    }

                }
            }

            return _fileProviders[upstream];
        }
    }
}