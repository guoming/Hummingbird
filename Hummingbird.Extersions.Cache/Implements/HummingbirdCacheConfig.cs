using System;
using System.Collections.Generic;
using System.Text;

namespace Hummingbird.Extersions.Cache
{
    public class HummingbirdCacheConfig : IHummingbirdCacheConfig
    {
        /// <summary>
        /// 分区前缀
        /// </summary>
        public string CacheRegion { get; set; }

#if NETCORE
        /// <summary>
        /// 配置名称
        /// </summary>
        public string ConfigName { get; set; } = "HummingbirdCache";
#endif


    }
}
