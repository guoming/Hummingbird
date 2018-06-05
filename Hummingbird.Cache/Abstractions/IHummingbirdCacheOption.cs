using System;
using System.Collections.Generic;
using System.Text;

namespace Hummingbird.Cache
{
    public interface IHummingbirdCacheOption
    {
        /// <summary>
        /// 分区前缀
        /// </summary>
        string regionPrefix { get; set; }

        /// <summary>
        /// 配置名称
        /// </summary>
        string configName { get; set; } 
    }
}
