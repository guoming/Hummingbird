using System;
using System.Collections.Generic;
using System.Text;

namespace Hummingbird.Extersions.Cache
{
    public interface IHummingbirdCacheOption
    {
        /// <summary>
        /// 分区前缀(推荐使用服务名称)
        /// </summary>
        string CacheRegion { get; set; }

        /// <summary>
        /// 配置名称（默认：HummingbirdCache）
        /// </summary>
        string ConfigName { get; set; } 
    }
}
