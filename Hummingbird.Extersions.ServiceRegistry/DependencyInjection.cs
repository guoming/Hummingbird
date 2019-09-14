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
using Microsoft.Extensions.Logging;

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
            var lifetime = hostBuilder.app.ApplicationServices.GetRequiredService<IApplicationLifetime>();
            var env = hostBuilder.app.ApplicationServices.GetRequiredService<IHostingEnvironment>();
            var configuration = hostBuilder.app.ApplicationServices.GetRequiredService<IConfiguration>();
            var logger = hostBuilder.app.ApplicationServices.GetRequiredService<ILogger<ConsulClient>>();

            try
            {

                var serviceConfig = new ServiceConfig();

                if (setup != null)
                {
                    setup(serviceConfig);
                }


                var policy = RetryPolicy.Handle<Exception>().Or<System.IO.IOException>()
                         .WaitAndRetryForever(a => { return TimeSpan.FromSeconds(5); }, (ex, time) =>
                         {
                             logger.LogError(ex, ex.Message);

                         });

                if (string.IsNullOrEmpty(serviceConfig.SERVICE_SELF_REGISTER) || serviceConfig.SERVICE_SELF_REGISTER.ToLower() == bool.TrueString.ToString().ToLower())
                {
                    var client = new ConsulClient(obj =>
                    {
                        obj.Address = new Uri($"http://{serviceConfig.SERVICE_REGISTRY_ADDRESS}:{serviceConfig.SERVICE_REGISTRY_PORT}");
                        obj.Datacenter = serviceConfig.SERVICE_REGION;
                        obj.Token = serviceConfig.SERVICE_REGISTRY_TOKEN;
                    });

                    var registrations = new List<AgentServiceRegistration>();

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
                            foreach (var ipEndPoint in _ips)
                            {
                                var registration = new AgentServiceRegistration()
                                {
                                    ID = $"{serviceConfig.SERVICE_NAME}:{ ipEndPoint}:{_port}",
                                    Name = serviceConfig.SERVICE_NAME,
                                    Address = ipEndPoint,
                                    Port = _port,
                                    Tags = new[] { serviceConfig.SERVICE_TAGS, env.EnvironmentName, env.ApplicationName },
                                    EnableTagOverride = true

                                };
                                var checks = new List<AgentServiceCheck>();
                                if (!string.IsNullOrEmpty(serviceConfig.SERVICE_80_CHECK_HTTP))
                                {
                                    checks.Add(new AgentServiceCheck()
                                    {
                                        Status = HealthStatus.Critical,
                                        HTTP = $"http://{ipEndPoint}:{_port}/{serviceConfig.SERVICE_80_CHECK_HTTP.TrimStart('/')}",
                                        Interval = TimeSpan.FromSeconds(int.Parse(serviceConfig.SERVICE_80_CHECK_INTERVAL.TrimEnd('s'))), //5秒执行一次健康检查
                                        Timeout = TimeSpan.FromSeconds(int.Parse(serviceConfig.SERVICE_80_CHECK_TIMEOUT.TrimEnd('s'))),//超时时间3秒
                                        DeregisterCriticalServiceAfter = TimeSpan.FromSeconds(int.Parse(serviceConfig.SERVICE_80_CHECK_INTERVAL.TrimEnd('s')) * 3), //2个心跳包结束
                                    });
                                }
                                else if (!string.IsNullOrEmpty(serviceConfig.SERVICE_CHECK_TCP))
                                {
                                    checks.Add(new AgentServiceCheck()
                                    {
                                        Status = HealthStatus.Critical,
                                        TCP = serviceConfig.SERVICE_CHECK_TCP,
                                        Interval = TimeSpan.FromSeconds(int.Parse(serviceConfig.SERVICE_CHECK_INTERVAL.TrimEnd('s'))), //5秒执行一次健康检查
                                        Timeout = TimeSpan.FromSeconds(int.Parse(serviceConfig.SERVICE_CHECK_TIMEOUT.TrimEnd('s'))),//超时时间3秒
                                        DeregisterCriticalServiceAfter = TimeSpan.FromSeconds(int.Parse(serviceConfig.SERVICE_CHECK_INTERVAL.TrimEnd('s')) * 3), //2个心跳包结束
                                    });
                                }
                                else if (!string.IsNullOrEmpty(serviceConfig.SERVICE_CHECK_SCRIPT))
                                {
                                    checks.Add(new AgentServiceCheck()
                                    {
                                        Status = HealthStatus.Critical,
                                        Script = serviceConfig.SERVICE_CHECK_SCRIPT,
                                        Interval = TimeSpan.FromSeconds(int.Parse(serviceConfig.SERVICE_CHECK_INTERVAL.TrimEnd('s'))), //5秒执行一次健康检查
                                        Timeout = TimeSpan.FromSeconds(int.Parse(serviceConfig.SERVICE_CHECK_TIMEOUT.TrimEnd('s'))),//超时时间3秒
                                        DeregisterCriticalServiceAfter = TimeSpan.FromSeconds(int.Parse(serviceConfig.SERVICE_CHECK_INTERVAL.TrimEnd('s')) * 3), //2个心跳包结束 
                                    });
                                }
                                else if (serviceConfig.SERVICE_CHECK_TTL.HasValue)
                                {
                                    checks.Add(new AgentServiceCheck()
                                    {
                                        Status = HealthStatus.Critical,
                                        TTL = TimeSpan.FromSeconds(serviceConfig.SERVICE_CHECK_TTL.Value),
                                    });
                                }

                                registration.Checks = checks.ToArray();

                                registrations.Add(registration);
                            }
                        }
                        else
                        {
                            logger.LogWarning("No registration service. port invalid");
                        }
                    }
                    else
                    {
                        var registration = new AgentServiceRegistration()
                        {
                            ID = $"{serviceConfig.SERVICE_NAME}",
                            Name = serviceConfig.SERVICE_NAME,
                            Tags = new[] { serviceConfig.SERVICE_TAGS, env.EnvironmentName, env.ApplicationName },
                            EnableTagOverride = true

                        };
                        var checks = new List<AgentServiceCheck>();

                         if (!string.IsNullOrEmpty(serviceConfig.SERVICE_CHECK_TCP))
                        {
                            checks.Add(new AgentServiceCheck()
                            {
                                Status = HealthStatus.Critical,
                                TCP = serviceConfig.SERVICE_CHECK_TCP,
                                Interval = TimeSpan.FromSeconds(int.Parse(serviceConfig.SERVICE_CHECK_INTERVAL.TrimEnd('s'))), //5秒执行一次健康检查
                                Timeout = TimeSpan.FromSeconds(int.Parse(serviceConfig.SERVICE_CHECK_TIMEOUT.TrimEnd('s'))),//超时时间3秒
                                DeregisterCriticalServiceAfter = TimeSpan.FromSeconds(int.Parse(serviceConfig.SERVICE_CHECK_INTERVAL.TrimEnd('s')) * 3), //2个心跳包结束
                            });
                        }
                        else if (!string.IsNullOrEmpty(serviceConfig.SERVICE_CHECK_SCRIPT))
                        {
                            checks.Add(new AgentServiceCheck()
                            {
                                Status = HealthStatus.Critical,
                                Script = serviceConfig.SERVICE_CHECK_SCRIPT,
                                Interval = TimeSpan.FromSeconds(int.Parse(serviceConfig.SERVICE_CHECK_INTERVAL.TrimEnd('s'))), //5秒执行一次健康检查
                                Timeout = TimeSpan.FromSeconds(int.Parse(serviceConfig.SERVICE_CHECK_TIMEOUT.TrimEnd('s'))),//超时时间3秒
                                DeregisterCriticalServiceAfter = TimeSpan.FromSeconds(int.Parse(serviceConfig.SERVICE_CHECK_INTERVAL.TrimEnd('s')) * 3), //2个心跳包结束 
                            });
                        }
                        else if (serviceConfig.SERVICE_CHECK_TTL.HasValue)
                        {
                            checks.Add(new AgentServiceCheck()
                            {
                                Status = HealthStatus.Critical,
                                TTL = TimeSpan.FromSeconds(serviceConfig.SERVICE_CHECK_TTL.Value),
                            });
                        }

                        registration.Checks = checks.ToArray();
                        registrations.Add(registration);
                    }


                    lifetime.ApplicationStarted.Register(() =>
                    {
                        foreach (var registration in registrations)
                        {
                            policy.Execute(async () =>
                            {
                                logger.LogInformation($"service {registration.ID} registration");

                                var ret = await client.Agent.ServiceRegister(registration);

                                logger.LogInformation($"service {registration.ID} registered. time={ret.RequestTime},statusCode={ret.StatusCode}");

                            });
                        }

                        if (serviceConfig.SERVICE_CHECK_TTL.HasValue && !string.IsNullOrEmpty(serviceConfig.SERVICE_CHECK_INTERVAL))
                        {
                            try
                            {
                                var timer = new System.Timers.Timer(int.Parse(serviceConfig.SERVICE_CHECK_INTERVAL) * 1000);
                                timer.Elapsed += new System.Timers.ElapsedEventHandler(async delegate (object sender, System.Timers.ElapsedEventArgs e)
                                {
                                    foreach (var registration in registrations)
                                    {
                                        try
                                        {
                                            await client.Agent.PassTTL("service:" + registration.ID, "");

                                            logger.LogDebug($"service {registration.ID} ttl passing");
                                        }
                                        catch
                                        { }

                                    }
                                });
                                timer.Start();
                            }
                            catch
                            { }
                        }

                    });

                    lifetime.ApplicationStopping.Register(() =>
                    {
                        try
                        {
                            foreach (var registration in registrations)
                            {
                                policy.Execute(async () =>
                                {
                                    logger.LogInformation($"service {registration.ID} deregister");
                                    //服务停止时取消注册
                                    var ret = await client.Agent.ServiceDeregister(registration.ID);

                                    logger.LogInformation($"service {registration.ID} Deregistered. time={ret.RequestTime},statusCode={ret.StatusCode}");
                                    return Task.FromResult(true);
                                });
                            }
                        }
                        catch (Exception ex)
                        { }

                    });

                    AppDomain.CurrentDomain.ProcessExit += new EventHandler(delegate (object sender, EventArgs e)
                    {

                        foreach (var registration in registrations)
                        {
                            policy.Execute(async () =>
                            {
                                logger.LogInformation($"service {registration.ID} deregister");
                                //服务停止时取消注册
                                var ret = await client.Agent.ServiceDeregister(registration.ID);

                                logger.LogInformation($"service {registration.ID} Deregistered. time={ret.RequestTime},statusCode={ret.StatusCode}");

                                return Task.FromResult(true);
                            });
                        }

                    });

                }
                else
                {
                    logger.LogWarning("No registration service");
                }

            }
            catch(Exception ex)
            {
                logger.LogError(ex, ex.Message);
            }

            return hostBuilder;
        }

   
    }
}
