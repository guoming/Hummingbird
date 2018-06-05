using Consul;
using Microsoft.AspNetCore.Builder;
using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.Threading.Tasks;
using Polly;
using Polly.Retry;
using Microsoft.Extensions.Options;

namespace Hummingbird.Core
{
    public static class DependencyInjection
    {
        public static ServiceConfig _serviceConfig;

        /// <summary>
        /// 使用服务注册
        /// 作者：郭明
        /// 日期：2017年10月30日
        /// </summary>
        /// <param name="app"></param>
        /// <param name="configuration"></param>
        public static void UseMicroService(this IApplicationBuilder app)
        {
            AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;

            _serviceConfig = _serviceConfig ?? new ServiceConfig();

            var configuration = app.ApplicationServices.GetRequiredService<IConfiguration>();

            var options = app.ApplicationServices.GetRequiredService<IOptions<ServiceConfig>>();

            _serviceConfig = options.Value;

            var policy = RetryPolicy.Handle<Exception>()
                .Or<System.IO.IOException>()
                .WaitAndRetryForever(a => { return TimeSpan.FromSeconds(5); }, (ex, time) =>
                {   
                    Console.WriteLine("WaitAndRetryForever" + ex.Message);
                });

            policy.Execute(() =>
            {
                var self_Register = _serviceConfig.SERVICE_SELF_REGISTER;
                _serviceConfig.SERVICE_REGISTRY_ADDRESS=_serviceConfig.SERVICE_REGISTRY_ADDRESS.Trim();

                if (self_Register == null || self_Register.ToLower() == bool.TrueString.ToString().ToLower())
                {
                    var client = new ConsulClient(obj =>
                    {
                        obj.Address = new Uri($"http://{_serviceConfig.SERVICE_REGISTRY_ADDRESS}:{_serviceConfig.SERVICE_REGISTRY_PORT}");
                        obj.Datacenter = _serviceConfig.SERVICE_REGION;
                    });

                    //计算健康检查地址
                    var SERVICE_80_CHECK_HTTP = _serviceConfig.SERVICE_80_CHECK_HTTP;

                    if (!SERVICE_80_CHECK_HTTP.Contains("http://"))
                    {
                        SERVICE_80_CHECK_HTTP = $"http://{_serviceConfig.SERVICE_ADDRESS}:{_serviceConfig.SERVICE_PORT}/{SERVICE_80_CHECK_HTTP.TrimStart('/')}";
                    }
                    
                    var result = client.Agent.ServiceRegister(new AgentServiceRegistration()
                    {
                        
                        ID = $"{_serviceConfig.SERVICE_NAME}|{_serviceConfig.SERVICE_ADDRESS}:{_serviceConfig.SERVICE_PORT}",
                        Name = _serviceConfig.SERVICE_NAME,
                        Address = _serviceConfig.SERVICE_ADDRESS,
                        Port = int.Parse(_serviceConfig.SERVICE_PORT),
                        Tags = new[] { _serviceConfig.SERVICE_TAGS },
                        EnableTagOverride=false,                          
                        Check = new AgentServiceCheck()
                        {  
                            Status= HealthStatus.Passing,
                            HTTP = SERVICE_80_CHECK_HTTP,
                            Interval = TimeSpan.FromSeconds(int.Parse(_serviceConfig.SERVICE_80_CHECK_INTERVAL.TrimEnd('s'))), //5秒执行一次健康检查
                            Timeout = TimeSpan.FromSeconds(int.Parse(_serviceConfig.SERVICE_80_CHECK_TIMEOUT.TrimEnd('s'))),//超时时间3秒
                            TTL= TimeSpan.FromSeconds(int.Parse(_serviceConfig.SERVICE_80_CHECK_INTERVAL.TrimEnd('s'))*3),//生存周期3个心跳包
                            DeregisterCriticalServiceAfter =TimeSpan.FromSeconds(int.Parse(_serviceConfig.SERVICE_80_CHECK_INTERVAL.TrimEnd('s'))*3), //2个心跳包结束
                        }
                    }).Result;

                    Console.WriteLine($"ServiceRegister({ _serviceConfig.SERVICE_NAME}|{ _serviceConfig.SERVICE_ADDRESS}:{ _serviceConfig.SERVICE_PORT}");
                }
            });
        }

        /// <summary>
        /// 添加微服务依赖
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IServiceCollection AddMicroService(this IServiceCollection services, IConfiguration configuration,Action<ServiceConfig> setupServiceConfig)
        {
            _serviceConfig = _serviceConfig ?? new ServiceConfig();

            if (setupServiceConfig != null)
            {
                setupServiceConfig(_serviceConfig);
            }

            services.Configure<ServiceConfig>(configuration);
            return services;
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
                if (_serviceConfig!=null && (_serviceConfig.SERVICE_SELF_REGISTER == null || _serviceConfig.SERVICE_SELF_REGISTER.ToLower() == bool.TrueString.ToString().ToLower()))
                {
                    var client = new ConsulClient(obj =>
                    {
                        obj.Address = new Uri($"http://{_serviceConfig.SERVICE_REGISTRY_ADDRESS}:{_serviceConfig.SERVICE_REGISTRY_PORT}");
                        obj.Datacenter = _serviceConfig.SERVICE_REGION
        ;
                    });

                    var result = client.Agent.ServiceDeregister($"{_serviceConfig.SERVICE_NAME}|{ _serviceConfig.SERVICE_ADDRESS}:{ _serviceConfig.SERVICE_PORT}");

                    Console.WriteLine($"ServiceDeregister({ _serviceConfig.SERVICE_NAME}|{ _serviceConfig.SERVICE_ADDRESS}:{ _serviceConfig.SERVICE_PORT}");
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
