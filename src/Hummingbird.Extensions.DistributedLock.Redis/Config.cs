using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hummingbird.Extensions.DistributedLock
{
    /// <summary>
    /// Redis服务配置
    /// </summary>
    public class Config
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="WriteServerList">127.0.0.1:6378 或 node1@191.168.0.1.6378,node2@191.168.0.1.6378</param>
        public void WithServerList(string WriteServerList)
        {
            this.WriteServerList = WriteServerList;
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

        public void WithLockExpirySeconds(int LockExpirySeconds)
        {
            this.LockExpirySeconds = LockExpirySeconds;
        }
        
        /// <summary>
        /// 写入服务器列表
        /// </summary>
        internal string WriteServerList
        { get; private set; }

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

        /// <summary>
        /// 锁过期时间
        /// </summary>
        internal int LockExpirySeconds { get; set; }= 30;

    }
}
