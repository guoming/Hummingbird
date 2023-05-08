using System;
using System.Collections;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Consul;
using Microsoft.Extensions.Logging;


namespace Hummingbird.Extensions.DistributedLock.Consul
{
    public class ConsulDistributedLock : IDistributedLock
    {        
        private  static readonly object _syncRoot = new object();
        private readonly IConsulClient _client;
        private readonly string _appId;
        private readonly Hashtable _hashtable = new Hashtable();
        private readonly ILogger<ConsulDistributedLock> _logger;
        
        public ConsulDistributedLock(
            IConsulClient consulClient,
            ILogger<ConsulDistributedLock> logger,
            string appId)
        {
            _client = consulClient;
            _logger = logger;
            _appId = appId;
        }

        public  LockResult Enter(
            string lockName, 
            string lockToken,
            int retryAttemptMillseconds = 50,
            int retryTimes = 5)
        {
            CancellationTokenSource cancellationToken = new CancellationTokenSource();

            lock (_syncRoot)
            {
                var ret = _client.Session.Create(new SessionEntry()
                {
                    Behavior = SessionBehavior.Delete,
                    TTL = TimeSpan.FromSeconds(10)
                }).Result;

                //会话创建成功
                if (ret.StatusCode == HttpStatusCode.OK)
                {
                    //会话自动续约
                    _client.Session.RenewPeriodic(TimeSpan.FromSeconds(1), ret.Response, CancellationToken.None);

                    _hashtable.Add($"{lockName}:{lockToken}", ret.Response);

                    do
                    {
                        retryTimes--;

                        if (retryTimes < 0)
                        {
                            return new LockResult(false,cancellationToken, lockName,lockToken);
                        }

                        var rs = _client.KV.Acquire(new KVPair($"{_appId}/LOCK/{lockName}")
                            {
                                Value = Encoding.UTF8.GetBytes(lockToken), 
                                Session = ret.Response
                                
                            }).Result
                            .Response;

                        if (rs)
                        {
                            return new LockResult(true,cancellationToken, lockName,lockToken);
                        }
                        else
                        {
                            _logger.LogInformation( $"#sessionId={ret.Response}.#lock={lockName}/LOCK Failed, try again in {retryAttemptMillseconds}ms");
                            
                            System.Threading.Thread.Sleep(TimeSpan.FromMilliseconds(retryAttemptMillseconds));
                            
                            continue;
                        }

                    } while (retryTimes > 0);

                    return new LockResult(false,cancellationToken, lockName,lockToken);
                }
                else
                {
                    return new LockResult(false,cancellationToken, lockName,lockToken);
                }
            }
        }

        /// <summary>
        /// 释放锁
        /// </summary>
        /// <param name="lockName">锁名称</param>
        /// <param name="lockToken">锁Token，token匹配才能解锁</param>
        public  void Exit(LockResult lockResult)
        {
            if (lockResult != null)
            {
                lockResult.CancellationToken.Cancel();

                lock (_syncRoot)
                {
                    if (_hashtable.ContainsKey($"{lockResult.LockName}:{lockResult.LockToken}"))
                    {
                        var sessionId = _hashtable[$"{lockResult.LockName}:{lockResult.LockToken}"].ToString();

                        if (!string.IsNullOrEmpty((sessionId)))
                        {
                            var rs = _client.KV.Release(new KVPair($"{_appId}/LOCK/{lockResult.LockName}")
                            {
                                Value = Encoding.UTF8.GetBytes(lockResult.LockToken),
                                Session = sessionId
                            }).Result.Response;

                            _hashtable.Remove($"{lockResult.LockName}:{lockResult.LockToken}");

                            _logger.LogInformation($"release Lock {lockResult.LockName} successful");

                        }
                    }
                }
            }
        }
    }
}