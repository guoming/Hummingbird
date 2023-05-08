using Hummingbird.Extensions.Cacheing;
using Hummingbird.Extensions.Cacheing.StackExchange;
using Polly;
using System;
using System.Collections;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Timer = System.Timers.Timer;

namespace Hummingbird.Extensions.DistributedLock.Redis
{

    public class RedisDistributedLock : IDistributedLock
    {
        private readonly ICacheManager _cacheManager;
        private readonly TimeSpan _lockCacheExpiry;
        private readonly ILogger<RedisDistributedLock> _logger;
        private readonly System.Timers.Timer _timer;
        public RedisDistributedLock(ICacheManager cacheManager,ILogger<RedisDistributedLock> logger, TimeSpan lockCacheExpiry) {

            this._cacheManager = cacheManager;
            this._lockCacheExpiry = lockCacheExpiry;
            this._logger = logger;
            this._timer = new Timer(1000);
        }

        private string GetLockCacheKey(string lockName)
        {
            return "Lock:" + lockName;
        }


        /// <summary>
        /// 锁过期时间续期
        /// </summary>
        private async Task Renew(string cacheKey, CancellationTokenSource cancellationToken)
        {
            var i = 0;
            while (!cancellationToken.IsCancellationRequested)
            {
                _cacheManager.ExpireEntryAt(cacheKey, _lockCacheExpiry);

                _logger.LogInformation(
                    $"lock query {cacheKey} successfully, expires in {_lockCacheExpiry.TotalMilliseconds} millseconds #{i} ");

                await Task.Delay(3000);
                i++;
            }
        }

        /// <summary>
        /// 获取分布式锁
        /// 作者：郭明
        /// 日期：2017年9月17日
        /// </summary>
        /// <param name="lockName">锁名称</param>
        /// <param name="lockToken">锁Token</param>
        /// <param name="retryAttemptMillseconds">自旋锁重试间隔时间（默认50毫秒）</param>
        /// <param name="retryTimes">自旋重试次数(默认10次)</param>
        /// <returns></returns>
        public LockResult Enter(
            string lockName,
            string lockToken,
            int retryAttemptMillseconds = 50,
            int retryTimes = 5)
        {

            var cancellationToken = new CancellationTokenSource();
            
            if (_cacheManager != null)
            {
                var cacheKey = GetLockCacheKey(lockName);
             
                
                do
                {
                    if (!_cacheManager.LockTake(cacheKey, lockToken, _lockCacheExpiry))
                    {
                        retryTimes--;
                        if (retryTimes < 0)
                        {
                            return new LockResult(false,cancellationToken, lockName,lockToken);
                        }

                        if (retryAttemptMillseconds > 0)
                        {
                            _logger.LogInformation($"enter Lock {lockName} failure, try again {retryAttemptMillseconds} millseconds");
                            
                            //获取锁失败则进行锁等待
                            System.Threading.Thread.Sleep(retryAttemptMillseconds);
                        }
                    }
                    else
                    {
                        _logger.LogInformation($"enter Lock {lockName} successfully");

                        Renew(cacheKey, cancellationToken);
                        
                        return new LockResult(true, cancellationToken, lockName,lockToken);
                    }
                }
                while (retryTimes > 0);

              
            }

            //获取锁超时返回
            return new LockResult(false,cancellationToken, lockName,lockToken);

        }

        /// <summary>
        /// 释放锁
        /// </summary>
        /// <param name="lockName">锁名称</param>
        /// <param name="lockToken">锁Token，token匹配才能解锁</param>
        public void Exit(LockResult lockResult)
        {
            if (lockResult != null)
            {
                lockResult.CancellationToken.Cancel();

                if (_cacheManager != null)
                {
                    var polly = Policy.Handle<Exception>()
                        .WaitAndRetry(10, retryAttempt => TimeSpan.FromMilliseconds(Math.Pow(2, retryAttempt)),
                            (exception, timespan, retryCount, context) =>
                            {
                                _logger.LogError($"release Lock {lockResult.LockName} failure,{exception.Message}");
                            });

                    polly.Execute(() =>
                    {
                        var cacheKey = GetLockCacheKey(lockResult.LockName);
                        _cacheManager.LockRelease(cacheKey, lockResult.LockToken);
                        _logger.LogInformation($"release Lock {lockResult.LockName} successful");

                    });
                }
            }
        }
    }
}
