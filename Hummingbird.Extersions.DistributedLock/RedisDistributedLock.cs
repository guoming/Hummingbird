using System;
using Hummingbird.Extersions.DistributedLock.StackExchangeImplement;
using Polly;
namespace Hummingbird.Extersions.DistributedLock
{
    public static class DistributedLockFactory {

        public static IDistributedLock CreateRedisDistributedLock(string LockName, string LockToken, RedisCacheConfig config)
        {
            var cacheManager = RedisCacheManage.Create(config);
            return new RedisDistributedLock(cacheManager, LockName, LockToken);
        }
    }

    class RedisDistributedLock : IDistributedLock
    {
        private readonly ICacheManager _cacheManager;
        private readonly string _lockName;
        private readonly string _lockToken;

        public RedisDistributedLock(ICacheManager cacheManager,string LockName,string LockToken) {

            this._cacheManager = cacheManager;
            this._lockName = LockName;
            this._lockToken = LockToken;
        }

        /// <summary>
        /// 获取分布式锁
        /// 作者：郭明
        /// 日期：2017年9月17日
        /// </summary>
        /// <param name="lockName">锁名称</param>
        /// <param name="LockOutTime">过期时间</param>
        /// <param name="retryAttemptMillseconds">自旋锁重试间隔时间（默认50毫秒）</param>
        /// <param name="retryTimes">自旋重试次数(默认10次)</param>
        /// <returns></returns>
        public bool Enter(
            TimeSpan LockOutTime,
            int retryAttemptMillseconds = 50,
            int retryTimes = 5)
        {
            if (_cacheManager != null)
            {
                var cacheKey = "Lock:" + _lockName;

                do
                {
                    if (!_cacheManager.LockTake(cacheKey, _lockToken, LockOutTime))
                    {
                        retryTimes--;
                        if (retryTimes < 0)
                        {
                            return false;
                        }

                        if (retryAttemptMillseconds > 0)
                        {
                            Console.WriteLine($"Wait Lock {_lockName} to {retryAttemptMillseconds} millseconds");
                            //获取锁失败则进行锁等待
                            System.Threading.Thread.Sleep(retryAttemptMillseconds);
                        }
                    }
                    else
                    {
                        return true;
                    }
                }
                while (retryTimes > 0);
            }

            //获取锁超时返回
            return false;

        }

        /// <summary>
        /// 释放分布式锁
        /// </summary>
        /// <param name="lockName"></param>
        /// <returns></returns>
        public void Exit()
        {
            if (_cacheManager != null)
            {
                
                var polly = Policy.Handle<Exception>()
                    .WaitAndRetry(10, retryAttempt => TimeSpan.FromMilliseconds(Math.Pow(2, retryAttempt)), (exception, timespan, retryCount, context) =>
                    {
                        Console.WriteLine($"执行异常,重试次数：{retryCount},【异常来自：{exception.GetType().Name}】.");
                    });

                polly.Execute(() =>
                {
                    _cacheManager.LockRelease("Lock:" + _lockName, _lockToken);

                });
            }
        }
    }
}
