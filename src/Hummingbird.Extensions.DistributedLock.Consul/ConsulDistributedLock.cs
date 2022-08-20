using System;
using System.Collections;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Consul;


namespace Hummingbird.Extensions.DistributedLock.Consul
{
    public class ConsulDistributedLock : IDistributedLock
    {
        private readonly IConsulClient _client;
        private static object _syncRoot = new object();
        private string _appId;
        private Hashtable _hashtable = new Hashtable();

        public ConsulDistributedLock(IConsulClient consulClient,string AppId)
        {
            _appId = AppId;
            _client = consulClient;
        }

        public  bool Enter(string LockName, 
            string LockToken, 
            TimeSpan LockOutTime=default(TimeSpan),
            int retryAttemptMillseconds = 50,
            int retryTimes = 5)
        {
            lock (_syncRoot)
            {
                var ret = _client.Session.Create(new SessionEntry()
                {
                    ID = LockToken,
                    Behavior = SessionBehavior.Delete,
                    TTL = TimeSpan.FromSeconds(10)
                }).Result;

                //会话创建成功
                if (ret.StatusCode == HttpStatusCode.OK)
                {
                    //会话自动续约
                    _client.Session.RenewPeriodic(TimeSpan.FromSeconds(1), ret.Response, CancellationToken.None);

                    _hashtable.Add($"{LockName}:{LockToken}", ret.Response);

                    do
                    {
                        retryTimes--;

                        if (retryTimes < 0)
                        {
                            return false;
                        }

                        var rs = _client.KV.Acquire(new KVPair($"{_appId}/LOCK/{LockName}")
                                { Value = Encoding.UTF8.GetBytes(LockToken), Session = ret.Response }).Result
                            .Response;

                        if (rs)
                        {
                            return true;
                        }
                        else
                        {
                            Console.WriteLine(
                                $"#sessionId={ret.Response}.#lock={LockName}/LOCK Failed, try again in {retryAttemptMillseconds}ms");
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

        public  void Exit(string LockName, string LockToken)
        {
            lock (_syncRoot)
            {
                if (_hashtable.ContainsKey($"{LockName}:{LockToken}"))
                {
                    var sessionId = _hashtable[$"{LockName}:{LockToken}"].ToString();

                    if (!string.IsNullOrEmpty((sessionId)))
                    {
                        var rs = _client.KV.Release(new KVPair($"{_appId}/LOCK/{LockName}")
                        {
                            Value = Encoding.UTF8.GetBytes(LockToken),
                            Session = sessionId
                        }).Result.Response;
                        
                        _hashtable.Remove($"{LockName}:{LockToken}");
                    }
                }
            }
        }
    }
}