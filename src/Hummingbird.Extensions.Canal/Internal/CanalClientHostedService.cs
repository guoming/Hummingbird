using CanalSharp.Client;
using CanalSharp.Client.Impl;
using Hummingbird.Extensions.Canal.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Hummingbird.Extensions.Canal
{
    /// <summary>
    /// 轨迹数据采集后台服务
    /// </summary>
    internal class CanalClientHostedService : IHostedService
    {
        private readonly CanalConfig _cannalConfig;
        private readonly ILogger<CanalClientHostedService> _logger;
        private readonly IList<ICanalConnector> _canalConnectors;

        public CanalClientHostedService(
            CanalConfig cannalConfig,
            ILogger<CanalClientHostedService> logger)
        {
            _cannalConfig = cannalConfig;
            _logger = logger;
            _canalConnectors = new List<ICanalConnector>();
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                foreach (var subscribeInfo in _cannalConfig.Subscribes)
                {
                    var subscripter = System.Activator.CreateInstance(Type.GetType(subscribeInfo.Type)) as ISubscripter;                    

                    //创建一个简单 CanalClient 连接对象（此对象不支持集群）传入参数分别为 canal 地址、端口、destination、用户名、密码
                    var connector = CanalConnectors.NewSingleConnector(
                        subscribeInfo.ConnectionInfo.Address,
                        subscribeInfo.ConnectionInfo.Port,
                        subscribeInfo.ConnectionInfo.Destination,
                        subscribeInfo.ConnectionInfo.UserName,
                        subscribeInfo.ConnectionInfo.Passsword);

                    //连接 Canal
                    connector.Connect();
                 
                    connector.Subscribe(subscribeInfo.Filter);

                    _canalConnectors.Add(connector);

                    while (true)
                    {
                        //获取数据 1024表示数据大小 单位为字节
                        var message = connector.GetWithoutAck(subscribeInfo.BatchSize);
                        //批次id 可用于回滚
                        var batchId = message.Id;

                        if (batchId == -1 || message.Entries.Count <= 0)
                        {
                            Thread.Sleep(100);
                            continue;
                        }
                        else
                        {
                            var ret = subscripter.Process(message.Entries.Where(entry=>entry.EntryType == Com.Alibaba.Otter.Canal.Protocol.EntryType.Rowdata).Select(a => a.ToCanalEventEntry()).ToArray());

                            if (ret)
                            {
                                connector.Ack(batchId);
                            }
                        }
                    }

                }

              
            }
            catch
            { }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            if (_canalConnectors != null && _canalConnectors.Any())
            {
                foreach (var canalConnector in _canalConnectors)
                {
                    canalConnector.UnSubscribe();
                    canalConnector.Disconnect();
                }
            }
            return Task.FromResult(true);
        }
    }
}
