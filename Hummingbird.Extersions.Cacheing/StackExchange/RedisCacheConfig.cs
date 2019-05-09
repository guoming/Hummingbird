using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hummingbird.Extersions.Cacheing.StackExchange
{
    /// <summary>
    /// Redis服务配置
    /// </summary>
    public class RedisCacheConfig
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="ReadServerList">192.168.100.51:16378,192.168.100.51:26378 或 node1@191.168.0.1.16378,node2@191.168.0.1.16378,node1@191.168.0.1.26378,node2@191.168.0.1.26378</param>
        public void WithReadServerList(string ReadServerList)
        {
            this.ReadServerList = ReadServerList;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="WriteServerList">127.0.0.1:6378 或 node1@191.168.0.1.6378,node2@191.168.0.1.6378</param>
        public void WithWriteServerList(string WriteServerList)
        {
            this.WriteServerList = WriteServerList;
        }

        /// <summary>
        /// 哨兵列表
        /// </summary>
        public void WithSentineList(string SentineList)
        {
            this.SentineList = SentineList;

        }
        public void WithPassword(string Password)
        {
            this.Password = Password;
        }

        public void WithKeyPrefix(string KeyPrefix)
        {
            this.KeyPrefix = KeyPrefix;
        }

        public void WithSsl(bool Ssl)
        {
            this.Ssl = Ssl;
        }

        public void WithDb(int num)
        {
            this.DBNum = num;
        }

        /// <summary>
        /// 读服务器列表
        /// </summary>
        internal string ReadServerList
        { get; private set; }

        /// <summary>
        /// 写入服务器列表
        /// </summary>
        internal string WriteServerList
        { get; private set; }

        /// <summary>
        /// 哨兵列表
        /// </summary>
        internal string SentineList { get; private set; }

        /// <summary>
        /// 密码
        /// </summary>
        internal string Password
        { get; private set; }


        /// <summary>
        /// Key前缀
        /// </summary>
        internal string KeyPrefix { get; private set; }
        /// <summary>
        /// 是否SSL连接
        /// </summary>
        internal bool Ssl { get; private set; } = false;

        /// <summary>
        /// 默认数据库
        /// </summary>
        internal int DBNum { get; private set; } = 0;

    }
}
