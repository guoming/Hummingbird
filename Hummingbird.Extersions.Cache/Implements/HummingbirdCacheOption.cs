using System;
using System.Collections.Generic;
using System.Text;

namespace Hummingbird.Extersions.Cache
{
    public class HummingbirdCacheOption:IHummingbirdCacheOption
    {
        /// <summary>
        /// 分区前缀
        /// </summary>
        public string CacheRegion { get; set; }

        /// <summary>
        /// 配置名称
        /// </summary>
        public string ConfigName { get; set; } = "HummingbirdCache";

    }
}
