using System;
using System.Threading;

namespace Hummingbird.Extensions.DistributedLock
{
    public class LockResult: IDisposable
    {
        public LockResult(bool Acquired, CancellationTokenSource cancellationToken, String lockName,string lockToken)
        {
            this.Acquired = Acquired;
            this.CancellationToken = cancellationToken;
            this.LockName = lockName;
            this.LockToken = lockToken;
        }

        public bool Acquired { get; set; }

        public CancellationTokenSource CancellationToken { get; set; }
        
        
        public string LockName { get; set; }
        
        public string LockToken { get; set; }

        public void Dispose()
        {
            if (CancellationToken != null)
            {
                CancellationToken.Cancel();
            }
        }
    }

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
        LockResult Enter(
            string lockName,
            string lockToken, 
            int retryAttemptMillseconds = 10,
            int retryTimes = 5);
        
        /// <summary>
        /// 释放锁
        /// </summary>
        /// <param name="lockName">锁名称</param>
        /// <param name="lockToken">锁Token，token匹配才能解锁</param>
        void Exit(LockResult lockResult);
    }
}