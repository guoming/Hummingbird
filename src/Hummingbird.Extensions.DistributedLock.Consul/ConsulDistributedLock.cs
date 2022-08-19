using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Consul;


namespace Hummingbird.Extensions.DistributedLock.Consul
{
    public class ConsulDistributedLock : IDistributedLock
    {
        private readonly IConsulClient _client;
        private string _sessionId;
        private static object _syncRoot = new object();
        private string _appId;

        public ConsulDistributedLock(IConsulClient consulClient,string AppId)
        {
            _appId = AppId;
            _client = consulClient;
            CreateSession();
        }

        private void CreateSession()
        {
            if (string.IsNullOrEmpty(_sessionId))
            {
                lock (_syncRoot)
                {
                    if (string.IsNullOrEmpty(_sessionId))
                    {
                        while (true)
                        {
                            var ret = _client.Session.Create(new SessionEntry()
                            {
                                Behavior = SessionBehavior.Delete,
                                TTL = TimeSpan.FromSeconds(10)
                            }).Result;
                            
                            if (ret.StatusCode == HttpStatusCode.OK)
                            {
                                _client.Session.RenewPeriodic(TimeSpan.FromSeconds(1), ret.Response,
                                    CancellationToken.None);

                                this._sessionId = ret.Response;

                                #region Destory

                                AppDomain.CurrentDomain.ProcessExit += delegate
                                {
                                    _client.Session.Destroy(_sessionId);
                                };

                                #endregion

                                return;
                            }
                            else
                            {
                                System.Threading.Thread.Sleep(1000);
                                continue;
                            }
                        }
                    }
                }
            }
        }

        public  bool Enter(string LockName, 
            string LockToken="", 
            TimeSpan LockOutTime=default(TimeSpan),
            int retryAttemptMillseconds = 50,
            int retryTimes = 5)
        {
            do
            {
                retryTimes--;

                if (retryTimes < 0)
                {
                    return false;
                }
                
                var rs = _client.KV.Acquire(new KVPair($"{_appId}/{LockName}/LOCK") { Session = _sessionId }).Result
                    .Response;

                if (rs)
                {
                    return true;
                }
                else
                {
                    Console.WriteLine(
                        $"#sessionId={_sessionId}.#lock={LockName}/LOCK Failed, try again in 5 seconds");
                    System.Threading.Thread.Sleep(retryAttemptMillseconds);
                    continue;
                }

            } while (retryTimes > 0);

            return false;
        }

        public  void Exit(string LockName, string LockToken)
        {
            while (true)
            {
                var rs = _client.KV.Release(new KVPair($"{_appId}/{LockName}/LOCK") { Session = _sessionId }).Result.Response;
                
                if (rs)
                {
                    return;
                }
                else
                {
                    System.Threading.Thread.Sleep(1000);
                    continue;
                }
            }
        }
    }
}