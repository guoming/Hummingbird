using Hummingbird.Extensions.Cacheing;
using Hummingbird.Extensions.Cacheing.StackExchange;
using Polly;
using System;
using System.Collections;
using System.Linq;
using System.Timers;
using Microsoft.Extensions.Logging;

namespace Hummingbird.Extensions.DistributedLock.Redis
{

    class RedisDistributedLock : IDistributedLock
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
            this.Renew();
        }

        private string GetLockCacheKey(string lockName)
        {
            return "Lock:" + lockName;
        }

        private bool RenewEnsure()
        {
            return _cacheManager.LockTake("Locks:RenewRunning", "", TimeSpan.FromSeconds(5));
        }
        
        private void RenewFinished()
        { 
            _cacheManager.LockRelease("Locks:RenewRunning","");
        }


        /// <summary>
        /// 锁过期时间续期
        /// </summary>
        private void Renew()
        {
            _timer.Elapsed += (sender, args) =>
            {
                
                try
                {
                    if (RenewEnsure())
                    {
                        var keys = _cacheManager.ListRange<string>("Locks");

                        foreach (var key in keys)
                        {
                            var arrs = key.Split(':').ToArray();

                            if (arrs.Length == 2)
                            {
                                var cacheKey = GetLockCacheKey(arrs[1]);

                                if (_cacheManager.KeyExists(cacheKey))
                                {
                                    _cacheManager.ExpireEntryAt(cacheKey, _lockCacheExpiry);

                                    _logger.LogInformation(
                                        $"expire lock {arrs[1]} successfully, expires in {_lockCacheExpiry.TotalMilliseconds} millseconds  ");

                                }
                                else
                                {
                                    _cacheManager.ListRemove("Locks", key);
                                }

                            }
                            else
                            {
                                _cacheManager.ListRemove("Locks", key);
                            }
                        }

                        RenewFinished();
                        
                        _logger.LogInformation("renew successfully");
                    }
                    else
                    {
                        _logger.LogInformation("renew failure, other processes are in process");
                    }
                    
                }
                catch (Exception e)
                {
                   _logger.LogError(e,e.Message);
                }
              
            };
            _timer.Start();
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
        public bool Enter(
            string lockName,
            string lockToken,
            int retryAttemptMillseconds = 50,
            int retryTimes = 5)
        {
            
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
                            return false;
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
                        _cacheManager.ListLeftPush("Locks", $"{lockToken}:{lockName}");
                        
                        _logger.LogInformation($"enter Lock {lockName} successfully");
                        
                        return true;
                    }
                }
                while (retryTimes > 0);
            }

            //获取锁超时返回
            return false;

        }

        /// <summary>
        /// 释放锁
        /// </summary>
        /// <param name="lockName">锁名称</param>
        /// <param name="lockToken">锁Token，token匹配才能解锁</param>
        public void Exit(
            string lockName,
            string lockToken)
        {
            if (_cacheManager != null)
            {
                var polly = Policy.Handle<Exception>()
                    .WaitAndRetry(10, retryAttempt => TimeSpan.FromMilliseconds(Math.Pow(2, retryAttempt)), (exception, timespan, retryCount, context) =>
                    {                            
                        _logger.LogError($"release Lock {lockName} failure,{exception.Message}");
                    });

                polly.Execute(() =>
                {
                    var cacheKey = GetLockCacheKey(lockName);
                    _cacheManager.LockRelease(cacheKey, lockToken);
                    _cacheManager.ListRemove("Locks",  $"{lockToken}:{lockName}");
                    _logger.LogInformation($"release Lock {lockName} successful");

                });
            }
        }
    }
}
