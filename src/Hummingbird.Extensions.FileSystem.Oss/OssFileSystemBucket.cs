using System;
using System.IO;
using System.Threading.Tasks;
using Aliyun.OSS;
using ZT.RFS.Infrastructure.FileProvider;

namespace Hummingbird.Extensions.FileSystem.Oss
{
    public class OssFileSystemBucket: IFileSystemBucket
    {
        private readonly OssFileMetaInfoCacheManager _ossFileMetaInfoCacheManager;
        private readonly string _bucketName;
        private readonly string _objectPrefix;
        private readonly OssClient _ossClient;

        public OssFileSystemBucket(
            OssFileMetaInfoCacheManager ossFileMetaInfoCacheManager,
            Config.OssEndpoint connectionInfo )
        {
            this._ossFileMetaInfoCacheManager = ossFileMetaInfoCacheManager;
            this._bucketName = connectionInfo.BucketName;
            this._objectPrefix = connectionInfo.ObjectPrefix;
                
            this._ossClient = new OssClient(
                connectionInfo.Endpoint,
                connectionInfo.AccessKeyId,
                connectionInfo.AccessKeySecret);
        }
        
        public void Delete(string key)
        {
            var deleteObjectResult = _ossClient.DeleteObject(_bucketName, $"{_objectPrefix}{key}");
            _ossFileMetaInfoCacheManager.getMemoryCache().Remove(key);
        }
        
        public async Task UploadAsync(string key, String localSrcFullFilePath, bool deleteSrcFile)
        {
            var cacheKey = $"{_bucketName}_meta_{_objectPrefix}{key}";
            
            if (!File.Exists(localSrcFullFilePath))
            {
                return; 
            }

            await PutObjectAsync(_bucketName, $"{_objectPrefix}{key}", localSrcFullFilePath);
            
            if (deleteSrcFile)
            {
                File.Delete(localSrcFullFilePath);
            }
            
            _ossFileMetaInfoCacheManager.getMemoryCache().Remove(cacheKey);
        }
        
         Task<PutObjectResult> PutObjectAsync(string bucketName, string objectName, string filePath)
         {
            return Task.Factory.FromAsync(
                _ossClient.BeginPutObject,
                _ossClient.EndPutObject,
                bucketName,
                objectName,
                filePath,
                null /* state object */);
        }
    }
}