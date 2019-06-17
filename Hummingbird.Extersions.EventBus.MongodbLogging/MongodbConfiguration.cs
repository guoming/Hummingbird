using System;
using System.Collections.Generic;
using System.Text;

namespace Hummingbird.Extersions.EventBus.MongodbLogging
{
    public class MongodbConfiguration
    {
        internal string ConnectionString { get; set; }

        internal string DatabaseName { get; set; }

        /// <summary>
        /// 超时时间
        /// </summary>
        internal int TimeoutMillseconds { get; set; } = 1000 * 20;

        /// <summary>
        /// 集合前缀
        /// </summary>
        internal string CollectionPrefix { get; set; } = "";

        public void WithEndpoint(string ConnectionString,string DatabaseName)
        {
            this.ConnectionString = ConnectionString;
            this.DatabaseName = DatabaseName;
        }

        public void WithSchema(string CollectionPrefix="")
        {
            this.CollectionPrefix = CollectionPrefix;
        }

        public void WithQos(int TimeoutMillseconds=1000*20)
        {
            this.TimeoutMillseconds = TimeoutMillseconds;
        }

    }
}
