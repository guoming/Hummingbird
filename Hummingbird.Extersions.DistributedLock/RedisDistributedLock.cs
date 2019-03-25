using System;
using Hummingbird.Extersions.DistributedLock.StackExchangeImplement;
using Polly;
namespace Hummingbird.Extersions.DistributedLock
{
    public static class DistributedLockFactory {

        public static IDistributedLock CreateRedisDistributedLock(RedisCacheConfig config)
        {
            var cacheManager = RedisCacheManage.Create(config);
            return new RedisDistributedLock(cacheManager);
        }
    }

    class RedisDistributedLock : IDistributedLock
    {
        private readonly ICacheManager _cacheManager;

        public RedisDistributedLock(ICacheManager cacheManager) {

            
            this._cacheManager = cacheManager;
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
            string LockName,
            string LockToken,
            TimeSpan LockOutTime,
            int retryAttemptMillseconds = 50,
            int retryTimes = 5)
        {
            if (_cacheManager != null)
            {
                var cacheKey = "Lock:" + LockName;

                do
                {
                  

                    if (!_cacheManager.LockTake(cacheKey, LockToken, LockOutTime))
                    {
                        retryTimes--;
                        if (retryTimes < 0)
                        {
                            return false;
                        }

                        if (retryAttemptMillseconds > 0)
                        {
                            Console.WriteLine($"Wait Lock {LockName} to {retryAttemptMillseconds} millseconds");
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
        public void Exit(
            string LockName,
            string LockToken)
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
                    _cacheManager.LockRelease("Lock:" + LockName, LockToken);

                });
            }
        }
    }
}
