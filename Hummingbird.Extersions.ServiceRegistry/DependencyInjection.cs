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
using Microsoft.AspNetCore.Hosting;
using System.Linq;
using System.Net.NetworkInformation;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class DependencyInjection
    { 
        /**获取ip地址*/
        public static List<string> getIps()
        {
            var ips = new List<string>();
            var addressList = Dns.GetHostEntry(Dns.GetHostName()).AddressList;//IP获取一个LIST里面有一个是IP
            for (int i = 0; i < addressList.Length; i++)
            {
                //判断是否为IP的格式
                if (System.Text.RegularExpressions.Regex.IsMatch(Convert.ToString(addressList[i]), @"((2[0-4]\d|25[0-5]|[01]?\d\d?)\.){3}(2[0-4]\d|25[0-5]|[01]?\d\d?)") == true)
                {
                    ips.Add(addressList[i].ToString());

                }
            }
            return ips;

        }
        /// <summary>
        /// 添加微服务依赖
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IHummingbirdApplicationBuilder UseServiceRegistry(this IHummingbirdApplicationBuilder hostBuilder, Action<ServiceConfig> setup)
        {
            var serviceConfig = new ServiceConfig();
            var lifetime = hostBuilder.app.ApplicationServices.GetRequiredService<IApplicationLifetime>();
            var env = hostBuilder.app.ApplicationServices.GetRequiredService<IHostingEnvironment>();
            var configuration = hostBuilder.app.ApplicationServices.GetRequiredService<IConfiguration>();

            if (setup != null)
            {
                setup(serviceConfig);
            }

            var urls = configuration["urls"].TrimEnd('/');
            if (urls.Contains("/") && urls.Contains(":"))
            {
                var str = urls.Split('/').LastOrDefault().Split(':');
                var _ip = str.FirstOrDefault();
                var _port = Convert.ToInt32(str.LastOrDefault());
                var _ips = new List<string>();
                _ips.AddRange(getIps());
                if (!_ips.Contains(_ip))
                {
                    _ips.Add(_ip);
                }

                if (_port > 0)
                {

                    var policy = RetryPolicy.Handle<Exception>().Or<System.IO.IOException>()
                       .WaitAndRetryForever(a => { return TimeSpan.FromSeconds(5); }, (ex, time) =>
                       {
                           Console.WriteLine("WaitAndRetryForever" + ex.Message);
                       });
                    if (string.IsNullOrEmpty(serviceConfig.SERVICE_SELF_REGISTER) || serviceConfig.SERVICE_SELF_REGISTER == bool.TrueString.ToString().ToLower())
                    {
                        var client = new ConsulClient(obj =>
                        {
                            obj.Address = new Uri($"http://{serviceConfig.SERVICE_REGISTRY_ADDRESS}:{serviceConfig.SERVICE_REGISTRY_PORT}");
                            obj.Datacenter = serviceConfig.SERVICE_REGION;
                            obj.Token = serviceConfig.SERVICE_REGISTRY_TOKEN;
                        });

                        policy.Execute(() =>
                        {

                            var registrations = new List<AgentServiceRegistration>();

                            foreach (var ipEndPoint in _ips)
                            {
                                registrations.Add(new AgentServiceRegistration()
                                {
                                    ID = $"{serviceConfig.SERVICE_NAME}:{ _ip}:{_port}",
                                    Name = serviceConfig.SERVICE_NAME,
                                    Address = _ip,
                                    Port = _port,
                                    Tags = new[] { serviceConfig.SERVICE_TAGS, env.EnvironmentName, env.ApplicationName },
                                    EnableTagOverride = true,
                                    Check = new AgentServiceCheck()
                                    {
                                        Status = HealthStatus.Passing,
                                        HTTP = $"http://{_ip}:{_port}/{serviceConfig.SERVICE_80_CHECK_HTTP.TrimStart('/')}",
                                        Interval = TimeSpan.FromSeconds(int.Parse(serviceConfig.SERVICE_80_CHECK_INTERVAL.TrimEnd('s'))), //5秒执行一次健康检查
                                        Timeout = TimeSpan.FromSeconds(int.Parse(serviceConfig.SERVICE_80_CHECK_TIMEOUT.TrimEnd('s'))),//超时时间3秒
                                        TTL = TimeSpan.FromSeconds(int.Parse(serviceConfig.SERVICE_80_CHECK_INTERVAL.TrimEnd('s')) * 3),//生存周期3个心跳包
                                        DeregisterCriticalServiceAfter = TimeSpan.FromSeconds(int.Parse(serviceConfig.SERVICE_80_CHECK_INTERVAL.TrimEnd('s')) * 3), //2个心跳包结束
                                    }
                                });
                            }

                            foreach (var registration in registrations)
                            {
                                var result = client.Agent.ServiceRegister(registration).Result;
                            }

                            lifetime.ApplicationStopping.Register(() =>
                            {
                                foreach (var registration in registrations)
                                {
                                    policy.Execute(() =>
                                    {
                                    //服务停止时取消注册
                                    client.Agent.ServiceDeregister(registration.ID).Wait();
                                        return Task.FromResult(true);
                                    });
                                }
                            });

                        });
                    }
                }
            }

            return hostBuilder;
        }

 

    
    }
}
