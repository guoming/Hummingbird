using Consul;
using Hummingbird.Extensions.UidGenerator.Implements;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Hummingbird.Extensions.UidGenerator.WorkIdCreateStrategy
{
    class ConsulWorkIdCreateStrategy : IWorkIdCreateStrategy
    {     
        private static readonly object _syncRoot = new object();
        private readonly IConsulClient _client;
        private readonly string _appId;
        private readonly string _serviceId;
        private readonly string _resourceId;
        private readonly TimeSpan _sessionTTL;
        private readonly TimeSpan _sessionRenewTTL;
        private readonly ILogger<ConsulWorkIdCreateStrategy> _logger;
        private readonly int _centerId;
        private  string _sessionId;
        private  int? _workId;
        private readonly string _sessionName;
        
        public ConsulWorkIdCreateStrategy(
            IConsulClient consulClient,
            ILogger<ConsulWorkIdCreateStrategy> logger,
            int CenterId,
            string appId,
            TimeSpan sessionTTL,
            TimeSpan sessionRenewTTL)
        {
            this._client = consulClient;
            this._appId = appId;
            this._resourceId = $"workid/{this._appId}";
            this._sessionId = string.Empty;
            this._logger = logger;
            this._centerId = CenterId;
            this._sessionTTL = sessionTTL;
            this._sessionRenewTTL = sessionRenewTTL;
            this._sessionName = $"{System.Net.Dns.GetHostName()}-{Process.GetCurrentProcess().ProcessName}-{Process.GetCurrentProcess().Id}";
            
            try
            {
                CreateSession();
                
                AppDomain.CurrentDomain.ProcessExit += delegate
                {
                    Dispose();
                };
            }
            catch (Exception e)
            {
                logger.LogError(e, e.Message);
            }
        }


        public int GetCenterId()
        {
            return _centerId;
        }

        public async Task<int> GetWorkId()
        {
            CreateSession();
            
            return await CreateWorkId();
        }

        private  void CreateSession()
        {
            if (string.IsNullOrEmpty(_sessionId))
            {
                lock (_syncRoot)
                {
                    if (string.IsNullOrEmpty(_sessionId))
                    {
                            var ret = _client.Session.Create(new SessionEntry() {  Behavior = SessionBehavior.Delete,  TTL = _sessionTTL }).Result;
                            
                            if (ret.StatusCode == HttpStatusCode.OK)
                            {
                                this._sessionId = ret.Response;
                                
                                _client.Session.RenewPeriodic(_sessionRenewTTL, this._sessionId, CancellationToken.None); 
                                
                                return;
                            }
                            else
                            {
                              
                                _logger.LogError("Create Session failed");
                            }
                        
                    }
                }
            }
        }


        private async Task<int> CreateWorkId()
        {
            if (!_workId.HasValue)
            {
                while (true)
                {
                    
                    try
                    {
                        var rs = (await _client.KV.Acquire(new KVPair($"{_resourceId}/LOCK") {  Session = _sessionId })).Response;

                        if (rs)
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
                                if (int.TryParse(item.Key.Replace($"{_resourceId}/",""), out int id))
                                {
                                    workIdRange.Remove(id);
                                }
                            }
                            #endregion

                            //存在可用的workId
                            if (workIdRange.Any())
                            {
                                var ret = await _client.KV.Acquire(new KVPair($"{_resourceId}/{workIdRange.First()}") {  Session = _sessionId, Value = Encoding.UTF8.GetBytes(_sessionName) });

                                if (ret.StatusCode == HttpStatusCode.OK && !ret.Response)
                                {
                                    throw new Exception($"Failed to allocate workid, failed to set workid #{workIdRange.First()}");
                                }

                                _workId = workIdRange.First();
                            }
                            else
                            {
                                throw new Exception($"Failed to allocate workid, no workid available");
                            }

                            break;

                        }
                        else
                        {
                            Console.WriteLine($"#sessionId={_sessionId}.#lock={_resourceId}/LOCK Failed to allocate workid, try again in 5 seconds");
                            await System.Threading.Tasks.Task.Delay(1000);
                            continue;
                        }
                    }                    
                    finally
                    {
                        while(true)
                        {
                            var rs = (await _client.KV.Release(new KVPair($"{_resourceId}/LOCK") { Session = _sessionId })).Response;
                            if(rs)
                            {
                                break;
                            }
                            else
                            {
                                await System.Threading.Tasks.Task.Delay(1000);
                                continue;
                            }
                        }
                    }
                }

            }

            return _workId.Value;
        }

        public async void Dispose()
        {
            if (!string.IsNullOrEmpty(_sessionId))
            {
                //释放WorkId
                if (_workId.HasValue)
                {
                    while(true)
                    {
                        var rs=(await _client.KV.Release(new KVPair($"{_resourceId}/{_workId.Value}")
                        {
                            Session = _sessionId,
                            Value = Encoding.UTF8.GetBytes(DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"))
                        })).Response;
                        
                        if(rs)
                        {
                            break;
                        }
                        else
                        {
                            await System.Threading.Tasks.Task.Delay(5000);
                            continue;
                        }
                    }
                }
                
                //结束会话
                await _client.Session.Destroy(_sessionId);
            }
        }
    }
}
