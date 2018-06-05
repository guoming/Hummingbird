using System;
using System.Collections.Generic;
using System.Text;

namespace Hummingbird.Cache
{
    public class HummingbirdCacheOption:IHummingbirdCacheOption
    {
        /// <summary>
        /// 分区前缀
        /// </summary>
        public string regionPrefix { get; set; }

        /// <summary>
        /// 配置名称
        /// </summary>
        public string configName { get; set; } = "HummingbirdCache";

    }
}
