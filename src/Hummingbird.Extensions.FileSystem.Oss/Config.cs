using System;
using System.Collections.Generic;
using System.IO;

namespace Hummingbird.Extensions.FileSystem
{
    public class Config
    {

        /// <summary>
        /// 文件缓存(开关)
        /// </summary>
        public bool CacheLocalFileEnabled { get; set; } = true;
        
        /// <summary>
        /// 缓存本地目录
        /// </summary>
        public string CacheLocalPath
        {
            get;
            set;
        }
        
        /// <summary>
        /// 文件缓存(大文件不进行缓存)
        /// </summary>
        public long CacheLocalFileSizeLimit { get; set; }=1024 * 1024 * 20;
        
        /// <summary>
        /// 文件缓存(通过命中次数计算是否热点)
        /// </summary>
        public long CacheLocalFileIfHits { get; set; } = 5;

        /// <summary>
        /// 缓存 Oss 文件元数据
        /// </summary>
        public bool CacheOssFileMetaEnable { get; set; } = true;

        /// <summary>
        /// 缓存 Oss 文件元数据过期时间(秒)
        /// </summary>
        public int CacheOssFileMetaAbsoluteExpirationSeconds { get; set; } = 600;


        
        public String EndpointName { get; set; }
        

        public Dictionary<string, OssEndpoint> Endpoints { get; set; }
        
        
        /// <summary>
        /// 阿里云OSS配置
        /// </summary>
        public class OssEndpoint
        {
            /// <summary>
            /// 访问域名
            /// </summary>
            public string Endpoint { get; set; }
            /// <summary>
            /// 访问Key
            /// </summary>
            public string AccessKeyId { get; set; }
            /// <summary>
            /// 访问密钥
            /// </summary>
            public string AccessKeySecret { get; set; }
            /// <summary>
            /// 存储空间
            /// </summary>
            public string BucketName { get; set; }

            public string ObjectPrefix { get; set; }
        }
        
    }

}