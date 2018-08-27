using System;
using System.Collections.Generic;
using System.Text;

namespace Hummingbird.Extersions.Cache
{
    public interface IHummingbirdCacheConfig
    {
        /// <summary>
        /// 分区前缀(推荐使用服务名称)
        /// </summary>
        string CacheRegion { get; set; }

        #if NETCORE
        /// <summary>
        /// 配置名称（默认：HummingbirdCache）
        /// </summary>
        string ConfigName { get; set; }
        #endif
    }
}
