using System.Diagnostics;
using Aliyun.OSS.Common;
using Hummingbird.Extensions.FileSystem.Oss;
using Microsoft.Extensions.Caching.Memory;

namespace Hummingbird.Extensions.FileSystem
{
    using Aliyun.OSS;
    using Microsoft.Extensions.FileProviders;
    using Microsoft.Extensions.Primitives;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.IO;

    public class OssFileProvider : IFileProvider
    {
        private readonly OssClient _ossClient;
        private readonly PhysicalFileProvider _physicalFileProvider;
        private readonly Config _config;
        private readonly OssFileMetaInfoCacheManager _ossFileMetaInfoCacheManager;
        private readonly string _bucketName;
        private readonly string _objectPrefix;

        public OssFileProvider(
            PhysicalFileProvider physicalFileProvider,
            Config config,
            OssFileMetaInfoCacheManager ossFileMetaInfoCacheManager,
            string OssUpstream)
        {

            var aliOssSetting = config.Endpoints[OssUpstream];
            
            ClientConfiguration clientConfiguration = new ClientConfiguration()
            {
                    UseNewServiceClient = true,
                     EnableCrcCheck = true,
                     EnalbeMD5Check = false,
                     MaxErrorRetry = 0,
            };
            this._ossClient = new OssClient(
                aliOssSetting.Endpoint,
                aliOssSetting.AccessKeyId, 
                aliOssSetting.AccessKeySecret,
                clientConfiguration);
            
            this._physicalFileProvider = physicalFileProvider;
            this._config = config;
            this._ossFileMetaInfoCacheManager = ossFileMetaInfoCacheManager;
            this._bucketName = aliOssSetting.BucketName;
            this._objectPrefix = aliOssSetting.ObjectPrefix;
        }

        public IDirectoryContents GetDirectoryContents(string subpath)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 获取本地文件信息元数据
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        IFileInfo GetPhysicalFileInfo(string key)
        {
            return _physicalFileProvider.GetFileInfo(key);
        }

        /// <summary>
        /// 获取 oss 文件元数据
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        IFileInfo GetOssFileInfo(string key)
        {
            try
            {
                var metadata = _ossClient.GetObjectMetadata(_bucketName, $"{_objectPrefix}{key}");

                return new OssFileInfo
                (
                    _config,
                    _ossFileMetaInfoCacheManager,
                    _ossClient,
                    this._bucketName,
                    this._objectPrefix,
                    key,
                    metadata.ContentLength,
                    metadata.LastModified);
            }
            catch (Exception)
            {
                return new NotFoundFileInfo(key);
            }
        }

        /// <summary>
        /// 获取文件信息元数据缓存
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        IFileInfo GetCachedFileInfo(string key)
        {
            if (!_config.CacheOssFileMetaEnable)
            {
                return new NotFoundFileInfo(key);
            }
            
            var cacheKey = $"{_bucketName}_meta_{_objectPrefix}{key}";
            var cachedFileInfo = _ossFileMetaInfoCacheManager.getMemoryCache().Get<IFileInfo>(cacheKey);
            if (cachedFileInfo == null)
            {
                return new NotFoundFileInfo(key);
            }
            
            if (cachedFileInfo.GetType().Name == nameof(OssFileInfo))
            {
                var ossFileInfo = (OssFileInfo)cachedFileInfo;
                return ossFileInfo.Clone();
            }
            else
            { 
                return cachedFileInfo;
            }
        }
        
        /// <summary>
        /// 设置文件信息元数据缓存
        /// </summary>
        /// <param name="key"></param>
        /// <param name="fileInfo"></param>
        void SetCachedFileInfo(string key, IFileInfo fileInfo)
        {
            var cacheKey = $"{_bucketName}_meta_{_objectPrefix}{key}";
            _ossFileMetaInfoCacheManager.getMemoryCache().Set(cacheKey, fileInfo, _ossFileMetaInfoCacheManager.GetCacheAbsoluteExpiration());
        }

        public IFileInfo GetFileInfo(string subpath)
        {
            try
            {
                var key = subpath.TrimStart('/');
                IFileInfo  fileInfo= GetCachedFileInfo(key);
                if (fileInfo != null && fileInfo.Exists)
                {
                    SetCachedFileInfo(key,fileInfo);
                    return fileInfo;
                }

                fileInfo = GetPhysicalFileInfo(key);
                if (fileInfo!=null &&  fileInfo.Exists && fileInfo.Length>0)
                {
                    SetCachedFileInfo(key,fileInfo);
                    return fileInfo;
                }

                fileInfo= GetOssFileInfo(key);
                if (fileInfo!=null &&  fileInfo.Exists)
                {
                    SetCachedFileInfo(key,fileInfo);
                }

                return fileInfo;
            }
            catch (Exception e)
            {
                throw;
            }
            finally
            {
                
            }
        }
  
        public IChangeToken Watch(string filter)
        {
            throw new NotImplementedException();
        }
    }
}