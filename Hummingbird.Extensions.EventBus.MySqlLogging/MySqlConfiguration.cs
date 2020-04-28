using System;
using System.Collections.Generic;
using System.Text;

namespace Hummingbird.Extensions.EventBus.MySqlLogging
{
    public class MySqlConfiguration
    {
        internal string ConnectionString { get; set; }

        /// 超时时间
        /// </summary>
        internal int TimeoutMillseconds { get; set; } = 1000 * 20;

        /// <summary>
        /// 表前缀
        /// </summary>
        internal string TablePrefix { get; set; } = "";

        public void WithEndpoint(string ConnectionString)
        {
            this.ConnectionString = ConnectionString;
        }

        public void WithSchema(string TablePrefix="")
        {
            this.TablePrefix = TablePrefix;
        }
    }
}
