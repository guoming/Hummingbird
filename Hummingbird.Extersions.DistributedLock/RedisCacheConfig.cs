using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hummingbird.Extersions.DistributedLock
{
    /// <summary>
    /// Redis服务配置
    /// </summary>
    public class RedisCacheConfig
    {

        /// <summary>
        /// 
        /// </summary>
        /// <param name="WriteServerList">127.0.0.1:6378 或 node1@191.168.0.1.6378,node2@191.168.0.1.6378</param>
        /// <param name="ReadServerList">192.168.100.51:16378,192.168.100.51:26378 或 node1@191.168.0.1.16378,node2@191.168.0.1.16378,node1@191.168.0.1.26378,node2@191.168.0.1.26378</param>
        /// <param name="Password">123456</param>
        /// <param name="Ssl">RedisSsl连接(默认:false)</param>
        /// <param name="DbNum">Redis数据库（默认：0）</param>
        /// <param name="KeyPrefix">缓存前缀(默认：空)</param>
        public RedisCacheConfig(
            string WriteServerList,
            string ReadServerList,
            string Password,
            bool Ssl=false,
            int DbNum=0, 
            string KeyPrefix="")
        {
            this.WriteServerList = WriteServerList;
            this.ReadServerList = ReadServerList;
            this.Password = Password;
            this.Ssl = Ssl;
            this.DBNum = DBNum;
            this.KeyPrefix = KeyPrefix;
        }

        /// <summary>
        /// 读服务器列表
        /// </summary>
        public string ReadServerList
        { get; private set; }

        /// <summary>
        /// 写入服务器列表
        /// </summary>
        public string WriteServerList
        { get; private set; }

        /// <summary>
        /// 哨兵列表
        /// </summary>
        public string SentineList { get; private set; }

        /// <summary>
        /// 密码
        /// </summary>
        public string Password
        { get; private set; }


        /// <summary>
        /// Key前缀
        /// </summary>
        public string KeyPrefix { get; private set; }
        /// <summary>
        /// 是否SSL连接
        /// </summary>
        public bool Ssl { get; private set; }

        /// <summary>
        /// 默认数据库
        /// </summary>
        public int DBNum { get; private set; }

    }
}
