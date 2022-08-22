using System;

namespace Hummingbird.Extensions.DistributedLock
{
    public interface IDistributedLock
    {
        /// <summary>
        /// 获取分布式锁
        /// 作者：郭明
        /// 日期：2017年9月17日
        /// </summary>
        /// <param name="lockName">锁名称</param>
        /// <param name="lockToken">锁Token</param>
        /// <param name="retryAttemptMillseconds">自旋锁重试间隔时间（默认50毫秒）</param>
        /// <param name="retryTimes">自旋重试次数(默认10次)</param>
        /// <returns>是否加锁成功</returns>
        bool Enter(
            string lockName,
            string lockToken, 
            int retryAttemptMillseconds = 50,
            int retryTimes = 5);

     
        /// <summary>
        /// 释放锁
        /// </summary>
        /// <param name="lockName">锁名称</param>
        /// <param name="lockToken">锁Token，token匹配才能解锁</param>
        void Exit(
            string lockName,
            string lockToken);
    }
}