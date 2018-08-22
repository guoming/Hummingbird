using System;

namespace Hummingbird.Extersions.DistributedLock
{
    public interface IDistributedLock
    {
        /// <summary>
        /// 加锁
        /// </summary>
        /// <param name="LockOutTime">锁保持时间</param>
        /// <param name="retryAttemptMillseconds">获取锁失败c重试间隔</param>
        /// <param name="retryTimes">最大重试次数</param>
        /// <returns></returns>
        bool Enter( TimeSpan LockOutTime, int retryAttemptMillseconds = 50, int retryTimes = 5);

        /// <summary>
        /// 释放锁
        /// </summary>
        void Exit();
    }
}