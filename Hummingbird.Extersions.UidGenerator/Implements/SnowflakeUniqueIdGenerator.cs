using System;
using System.Collections.Generic;
using System.Text;

namespace Hummingbird.Extersions.UidGenerator
{
    /// <summary>
    /// Twiter SnowFlake唯一ID生成算法
    /// </summary>
    public class SnowflakeUniqueIdGenerator : IUniqueIdGenerator
    {
        readonly Snowflake.Core.IdWorker idWorker;
        public SnowflakeUniqueIdGenerator(int WorkerId, int CenterId)
        {
            idWorker = new Snowflake.Core.IdWorker(WorkerId, CenterId);
        }

        /// <summary>
        /// 生存唯一ID
        /// </summary>
        /// <param name="Prefix"></param>
        /// <returns></returns>
        public long NewId()
        {
            return idWorker.NextId();
        }
    }
}
