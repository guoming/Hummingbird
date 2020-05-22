using Consul;
using Hummingbird.DynamicRoute;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Hummingbird.Extensions.UidGenerator.WorkIdCreateStrategy
{
    public class ConsulWorkIdCreateStrategy : IWorkIdCreateStrategy
    {
        private readonly IServiceDiscoveryProvider _serviceDiscoveryProvider;
        private readonly IConsulClient _client;
        private readonly string _appId;
        private readonly string _serviceId;
        private readonly string _resourceId;
        private string _sessionId;
        private int? _workId;
        private static object _syncRoot = new object();
        
        public ConsulWorkIdCreateStrategy(
            IServiceDiscoveryProvider serviceDiscoveryProvider,
            IConsulClient consulClient,
            string appId)
        {
            this._serviceDiscoveryProvider = serviceDiscoveryProvider;
            this._client = consulClient;
            this._appId = appId;
            this._serviceId = _serviceDiscoveryProvider.ServiceId;
            this._resourceId = $"workId/{this._appId}";
            this._sessionId = string.Empty;

            CreateSession();

        }


        public async Task<int> NextId()
        {
            return await GetOrCreateWorkId();
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
                            var ret = _client.Session.Create(new SessionEntry() {  Behavior = SessionBehavior.Delete, TTL = TimeSpan.FromSeconds(30) }).Result;
                            if (ret.StatusCode == HttpStatusCode.OK)
                            {
                                this._sessionId = ret.Response;

                                #region Destory
                                AppDomain.CurrentDomain.ProcessExit += delegate
                                {
                                    _client.Session.Destroy(_sessionId);
                                };
                                #endregion

                                _client.Session.RenewPeriodic(TimeSpan.FromSeconds(5), _sessionId, CancellationToken.None);

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

        private async Task<int> GetOrCreateWorkId()
        {
            if (!_workId.HasValue)
            {

                while (true)
                {
                    var rs = (await _client.KV.Acquire(new KVPair($"{_resourceId}/LOCK") { Session = _sessionId })).Response;

                    if (rs)
                    {
                        var rs2 = (await _client.KV.Acquire(new KVPair($"{_resourceId}/{_serviceId}") { Session = _sessionId })).Response;

                        if (rs2)
                        {
                            var kvList = await _client.KV.List(_resourceId);

                            var workIdRange = new List<int> { };

                            for (int i = 0; i < IdWorker.MaxWorkerId; i++)
                            {
                                workIdRange.Add(i);
                            }

                            #region 排除已经存在workId
                            foreach (var item in kvList.Response)
                            {
                                var key = item.Key;
                                if (item.Value != null)
                                {
                                    var value = Encoding.UTF8.GetString(item.Value, 0, item.Value.Length);
                                    if (int.TryParse(value, out int id))
                                    {
                                        workIdRange.Remove(id);
                                    }
                                }
                            }
                            #endregion


                            //存在可用的workId
                            if (workIdRange.Any())
                            {
                                _workId = workIdRange.First();
                                var ret = await _client.KV.Put(new KVPair($"{_resourceId}/{_serviceId}") { Session = _sessionId, Value = Encoding.UTF8.GetBytes(_workId.ToString()) });

                                if (ret.StatusCode== HttpStatusCode.OK && !ret.Response)
                                {
                                    throw new Exception($"Failed to allocate workid, failed to set workid");
                                }
                            }
                            else
                            {
                                throw new Exception($"Failed to allocate workid, no workid available");
                            }

                            break;
                        }
                        else
                        {
                            Console.WriteLine($"#sessionId={_sessionId}.#lock={_resourceId}/{_serviceId}.Failed to allocate workid, try again in 5 seconds");
                            await System.Threading.Tasks.Task.Delay(5000);
                            continue;
                        }
                    }
                    else
                    {
                        Console.WriteLine($"#sessionId={_sessionId}.#lock={_resourceId}/LOCK Failed to allocate workid, try again in 5 seconds");
                        await System.Threading.Tasks.Task.Delay(5000);
                        continue;
                    }
                }

            }

            return _workId.Value;
        }
            
    }
}
