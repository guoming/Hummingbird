using System;
using System.IO;
using System.Threading.Tasks;
using Aliyun.OSS;
using Hummingbird.Extensions.FileSystem.Oss;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.FileProviders;


namespace Hummingbird.Extensions.FileSystem
{
    public class OssFileInfo : IFileInfo,IDisposable
    {
        private static object _syncRoot=new object();
        private readonly Config _config;
        private readonly OssFileMetaInfoCacheManager _ossFileMetaInfoCacheManager;
        private readonly OssClient _ossClient;
        private readonly string _bucketName;
        private readonly string _objectPrefix;
        private readonly string _name;
        private readonly long _length;
        private readonly DateTimeOffset _lastModified;
        private OssObject _ossObject;
        
        public OssFileInfo(
            Config config,
            OssFileMetaInfoCacheManager ossFileMetaInfoCacheManager,
            OssClient ossClient, 
            string BucketName,
            string ObjectPrefix,
            string Name,
            long Length, 
            DateTimeOffset LastModified)
        {
            this._config = config;
            this._ossFileMetaInfoCacheManager = ossFileMetaInfoCacheManager;
            this._ossClient = ossClient;
            this._bucketName = BucketName;
            this._objectPrefix = ObjectPrefix;
            this._name = Name.TrimStart('/');
            this._length = Length;
            this._lastModified = LastModified;
        }

        public bool Exists => true;

        public long Length => _length;

        public string PhysicalPath => null;

        public string Name => _name;

        public DateTimeOffset LastModified => _lastModified;

        public bool IsDirectory => false;

        /// <summary>
        /// 获取本地文件完整路径
        /// </summary>
        /// <returns></returns>
        String GetLocalFileFullPath()
        {
            //本地文件存在(本次磁盘或者挂载的 OSS)
            var localDataFullPath = Path.Combine(_config.CacheLocalPath, _name);
            return localDataFullPath;
        }

        /// <summary>
        /// 获取本地文件流
        /// </summary>
        /// <returns></returns>
        Stream GetLocalFileStream()
        {
            //本地文件存在(本次磁盘或者挂载的 OSS)
            var localDataFullPath = GetLocalFileFullPath();
            if (System.IO.File.Exists(localDataFullPath))
            {
                FileStream fileStream = new FileStream(localDataFullPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 1, FileOptions.SequentialScan | FileOptions.Asynchronous);

                //读取的流大小和元数据的文件大小一致则返回, 避免文件不完整
                if (fileStream.Length == _length)
                    return fileStream;
            }

            return null;
        }

        Stream GetOssFileStream()
        {
            //已经有缓存了直接返回
            if (_ossObject != null && _ossObject.Content != null)
            {
                var stream= _ossObject.Content;
                
                //读取的流大小和元数据的文件大小一致则返回, 避免文件不完整
                if (stream.Length == _length)
                    return stream;
            }

            return null;
        }
        

        async Task<Stream> GetOrCreateOssFileStreamAsync()
        {
            //已经有缓存了直接返回
            if (_ossObject != null && _ossObject.Content != null)
            {
                return _ossObject.Content;
            }
            
            _ossObject = await GetObjectAsync(this._bucketName, $"{this._objectPrefix}{_name}");
            return _ossObject.Content;
        }
        
        
        Task<OssObject> GetObjectAsync(string bucketName, string filePath)
        {
            return Task.Factory.FromAsync(
                _ossClient.BeginGetObject,
                _ossClient.EndGetObject,
                bucketName,
                filePath,
                null /* state object */);
        }

        /// <summary>
        /// 写入文件缓存如果命中
        /// </summary>
        /// <returns></returns>
        async Task WriteLocalFileCache()
        {
            //未开启忽略
            if (!_config.CacheLocalFileEnabled)
            {
                return;
            }
            //小文件才缓存(大于阈值则忽略)
            if (_length > _config.CacheLocalFileSizeLimit)
            {
                return;
            }
            
            try
            {
                //文件存在忽略
                if (File.Exists(GetLocalFileFullPath()))
                {
                    return;
                } 
                //统计命中数量
                var hits_cacheKey = $"{_bucketName}_hits_{_objectPrefix}{_name}";
                //命中数量
                var hits_Count = _ossFileMetaInfoCacheManager.getMemoryCache()
                    .GetOrCreate<long>(hits_cacheKey, entry => { return 0; });

                //命中次数递增
                _ossFileMetaInfoCacheManager.getMemoryCache().Set<long>(hits_cacheKey, ++hits_Count);
                //命中次数不够忽略
                if (hits_Count < _config.CacheLocalFileIfHits)
                {
                    return;
                }

                var ossStream = await GetOrCreateOssFileStreamAsync();
                var directoryName = System.IO.Path.GetDirectoryName(GetLocalFileFullPath());

                if (!System.IO.Directory.Exists(directoryName))
                {
                    System.IO.Directory.CreateDirectory(directoryName);
                }

                using (var readStream = ossStream)
                {
                    using (var fs = new System.IO.FileStream(GetLocalFileFullPath(),
                               System.IO.FileMode.CreateNew))
                    {
                        await readStream.CopyToAsync(fs);
                        await fs.FlushAsync();
                    }
                }

                Dispose();
                
            }
            catch (Exception e)
            {
                throw;
            }
        }

        public Stream CreateReadStream()
        {
            return CreateReadStreamAsync().Result;
        }
        
        public async Task<Stream>CreateReadStreamAsync()
        {
            //文件不存在忽略
            if (!Exists)
            {
                return null;
            }

            var ossFileStream = GetOssFileStream();
            if (ossFileStream != null)
            {
                return ossFileStream;
            }

            var localFileStream = GetLocalFileStream();
            if (localFileStream != null)
                return localFileStream;
            
            //写入本地文件缓存
            await WriteLocalFileCache();
            
            localFileStream = GetLocalFileStream();
            if (localFileStream != null)
                return localFileStream;

            return await GetOrCreateOssFileStreamAsync();
        }
        
  

        public void Dispose()
        {
            if (this._ossObject != null)
            {
                this._ossObject.Dispose();
            }
        }
        
        
        public IFileInfo Clone()
        {
           return new OssFileInfo(
               _config,
               _ossFileMetaInfoCacheManager,
               _ossClient,
               _bucketName,
               _objectPrefix, 
               _name,
               _length, 
               _lastModified);
        }

        
    }
}