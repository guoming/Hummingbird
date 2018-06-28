using Consul;
using Microsoft.AspNetCore.Builder;
using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.Threading.Tasks;
using Polly;
using Polly.Retry;
using Hummingbird.Extersions.ServiceRegistry;
using Hummingbird.Core;
using System.Net;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class DependencyInjection
    {
        public static ServiceConfig _serviceConfig;

    

        /// <summary>
        /// 添加微服务依赖
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IHummingbirdHostBuilder AddServiceRegistry(this IHummingbirdHostBuilder hostBuilder, Action<ServiceConfig> setupServiceConfig)
        {
            AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;

            _serviceConfig = _serviceConfig ?? new ServiceConfig();

            if (setupServiceConfig != null)
            {
                setupServiceConfig(_serviceConfig);
            }


            var policy = RetryPolicy.Handle<Exception>()
                .Or<System.IO.IOException>()
                .WaitAndRetryForever(a => { return TimeSpan.FromSeconds(5); }, (ex, time) =>
                {
                    Console.WriteLine("WaitAndRetryForever" + ex.Message);
                });

            policy.Execute(() =>
            {
                var self_Register = _serviceConfig.SERVICE_SELF_REGISTER;
                _serviceConfig.SERVICE_REGISTRY_ADDRESS = _serviceConfig.SERVICE_REGISTRY_ADDRESS.Trim();

                if (self_Register == null || self_Register.ToLower() == bool.TrueString.ToString().ToLower())
                {
                    var client = new ConsulClient(obj =>
                    {
                        obj.Address = new Uri($"http://{_serviceConfig.SERVICE_REGISTRY_ADDRESS}:{_serviceConfig.SERVICE_REGISTRY_PORT}");
                        obj.Datacenter = _serviceConfig.SERVICE_REGION;
                        obj.Token = _serviceConfig.SERVICE_REGISTRY_TOKEN;
                    });

                    //计算健康检查地址
                    var SERVICE_80_CHECK_HTTP = _serviceConfig.SERVICE_80_CHECK_HTTP;

                    if (!SERVICE_80_CHECK_HTTP.Contains("http://"))
                    {
                        SERVICE_80_CHECK_HTTP = $"http://{_serviceConfig.SERVICE_ADDRESS}:{_serviceConfig.SERVICE_PORT}/{SERVICE_80_CHECK_HTTP.TrimStart('/')}";
                    }
              
                    var result = client.Agent.ServiceRegister(new AgentServiceRegistration()
                    {
                        ID = _serviceConfig.SERVICE_ID,
                        Name = _serviceConfig.SERVICE_NAME,
                        Address = _serviceConfig.SERVICE_ADDRESS,
                        Port = int.Parse(_serviceConfig.SERVICE_PORT),
                        Tags = new[] { _serviceConfig.SERVICE_TAGS },
                        EnableTagOverride = true,
                        Check = new AgentServiceCheck()
                        {
                            Status = HealthStatus.Passing,
                            HTTP = SERVICE_80_CHECK_HTTP,
                            Interval = TimeSpan.FromSeconds(int.Parse(_serviceConfig.SERVICE_80_CHECK_INTERVAL.TrimEnd('s'))), //5秒执行一次健康检查
                            Timeout = TimeSpan.FromSeconds(int.Parse(_serviceConfig.SERVICE_80_CHECK_TIMEOUT.TrimEnd('s'))),//超时时间3秒
                            //TTL = TimeSpan.FromSeconds(int.Parse(_serviceConfig.SERVICE_80_CHECK_INTERVAL.TrimEnd('s')) * 3),//生存周期3个心跳包
                            DeregisterCriticalServiceAfter = TimeSpan.FromSeconds(int.Parse(_serviceConfig.SERVICE_80_CHECK_INTERVAL.TrimEnd('s')) * 3), //2个心跳包结束
                        }
                    }).Result;

                    Console.WriteLine($"ServiceRegister({_serviceConfig.SERVICE_ID}");
                }
            });

            return hostBuilder;
        }

        static void UnRegister()
        {
            var policy = RetryPolicy.Handle<Exception>()
              .Or<System.IO.IOException>()
              .WaitAndRetryForever(a => { return TimeSpan.FromSeconds(5); }, (ex, time) =>
              {
                  Console.WriteLine("WaitAndRetryForever" + ex.Message);
              });

            policy.Execute(() =>
            {
                if (_serviceConfig != null && (_serviceConfig.SERVICE_SELF_REGISTER == null || _serviceConfig.SERVICE_SELF_REGISTER.ToLower() == bool.TrueString.ToString().ToLower()))
                {
                    var client = new ConsulClient(obj =>
                    {
                        obj.Address = new Uri($"http://{_serviceConfig.SERVICE_REGISTRY_ADDRESS}:{_serviceConfig.SERVICE_REGISTRY_PORT}");
                        obj.Datacenter = _serviceConfig.SERVICE_REGION
        ;
                    });

                    var ID = _serviceConfig.SERVICE_ID;

                    var result = client.Agent.ServiceDeregister(ID);

                    Console.WriteLine($"ServiceDeregister({ID}");
                }

                return Task.FromResult(true);

            });

        }

        static void CurrentDomain_ProcessExit(object sender, EventArgs e)
        {
            UnRegister();
        }
    }
}
