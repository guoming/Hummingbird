using System;
using System.Collections.Generic;
using System.Text;

namespace Hummingbird.Extensions.Canal
{

    public class CanalConfig
    {
        public SubscribeInfo[] Subscribes { get; set; }

        public class SubscribeInfo
        {
            /// <summary>
            /// 
            /// </summary>
            ///<example>
            //允许所有数据 .*\\..*
            //允许某个库数据 库名\\..*
            //允许某些表 库名.表名,库名.表名
            ///</example>
            public string Filter { get; set; } = ".*\\..*";

            public int BatchSize { get; set; } = 1024;

            public string Type { get; set; } = "";

            public CancalConnectionInfo ConnectionInfo { get; set; }

            public class CancalConnectionInfo
            {
                public string Address { get; set; } = "127.0.0.1";

                public int Port { get; set; } = 11111;

                public string Destination { get; set; } = "example";

                public string UserName { get; set; } = "";

                public string Passsword { get; set; } = "";
            }

        }
    }

  
}
