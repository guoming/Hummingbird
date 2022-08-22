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

        public  bool Enter(
            string lockName, 
            string lockToken,
            int retryAttemptMillseconds = 50,
            int retryTimes = 5)
        {
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
                            return false;
                        }

                        var rs = _client.KV.Acquire(new KVPair($"{_appId}/LOCK/{lockName}")
                            {
                                Value = Encoding.UTF8.GetBytes(lockToken), 
                                Session = ret.Response
                                
                            }).Result
                            .Response;

                        if (rs)
                        {
                            return true;
                        }
                        else
                        {
                            _logger.LogInformation( $"#sessionId={ret.Response}.#lock={lockName}/LOCK Failed, try again in {retryAttemptMillseconds}ms");
                            
                            System.Threading.Thread.Sleep(TimeSpan.FromMilliseconds(retryAttemptMillseconds));
                            
                            continue;
                        }

                    } while (retryTimes > 0);

                    return false;
                }
                else
                {
                    return false;
                }
            }
        }

        /// <summary>
        /// 释放锁
        /// </summary>
        /// <param name="lockName">锁名称</param>
        /// <param name="lockToken">锁Token，token匹配才能解锁</param>
        public  void Exit(string lockName, string lockToken)
        {
            lock (_syncRoot)
            {
                if (_hashtable.ContainsKey($"{lockName}:{lockToken}"))
                {
                    var sessionId = _hashtable[$"{lockName}:{lockToken}"].ToString();

                    if (!string.IsNullOrEmpty((sessionId)))
                    {
                        var rs = _client.KV.Release(new KVPair($"{_appId}/LOCK/{lockName}")
                        {
                            Value = Encoding.UTF8.GetBytes(lockToken),
                            Session = sessionId
                        }).Result.Response;
                        
                        _hashtable.Remove($"{lockName}:{lockToken}");
                        
                        _logger.LogInformation($"release Lock {lockName} successful");

                    }
                }
            }
        }
    }
}