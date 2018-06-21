using System;
using System.Collections.Generic;
using System.Text;

namespace Hummingbird.Extersions.EventBus.Models
{
    public enum EventConsumeStateEnum
    {
        /// <summary>
        /// 没有接收
        /// </summary>
        NotRecived = 0,
        /// <summary>
        /// 接收成功
        /// </summary>
        Reviced = 1,
        /// <summary>
        /// 消费失败
        /// </summary>
        RevicedFailed = 2
    }
}
