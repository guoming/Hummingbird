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
using System.Threading;
using System.IO;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class DependencyInjection
    { 
      
        public static IHummingbirdApplicationBuilder UseServiceRegistry(this IHummingbirdApplicationBuilder hostBuilder, Action<ServiceConfig> setup)
        {
            ServiceRegistryBootstraper.Register(hostBuilder.app.ApplicationServices, setup);
            return hostBuilder;
        }
    }



}

namespace Hummingbird.Extersions.ServiceRegistry
{
    public static class ServiceRegistryBootstraper
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

        public static void Register(IServiceProvider serviceProvider, Action<ServiceConfig> setup)
        {
            var lifetime = ServiceProviderServiceExtensions.GetRequiredService<IApplicationLifetime>(serviceProvider);
            var hosting = ServiceProviderServiceExtensions.GetRequiredService<IHostingEnvironment>(serviceProvider);
            var configuration = ServiceProviderServiceExtensions.GetRequiredService<IConfiguration>(serviceProvider);
            var logger = ServiceProviderServiceExtensions.GetRequiredService<ILogger<ConsulClient>>(serviceProvider);
            try
            {
                ServiceConfig serviceConfig = new ServiceConfig();
                setup?.Invoke(serviceConfig);
                RetryPolicy policy = Policy.Handle<Exception>().Or<IOException>().WaitAndRetryForever((int a) => TimeSpan.FromSeconds(5.0), delegate (Exception ex, TimeSpan time)
                {
                    logger.LogError(ex, ex.Message, Array.Empty<object>());
                });

                if (string.IsNullOrEmpty(serviceConfig.SERVICE_SELF_REGISTER) || serviceConfig.SERVICE_SELF_REGISTER.ToLower() == bool.TrueString.ToString().ToLower())
                {
                    ConsulClient client = new ConsulClient(delegate (ConsulClientConfiguration obj)
                    {
                        obj.Address = new Uri("http://" + serviceConfig.SERVICE_REGISTRY_ADDRESS + ":" + serviceConfig.SERVICE_REGISTRY_PORT);
                        obj.Datacenter = serviceConfig.SERVICE_REGION;
                        obj.Token = serviceConfig.SERVICE_REGISTRY_TOKEN;
                    });
                    List<AgentServiceRegistration> registrations = new List<AgentServiceRegistration>();
                    string text = configuration["urls"]?.TrimEnd('/');
                    if (text.Contains("/") && text.Contains(":"))
                    {
                        string[] source = text.Split('/', StringSplitOptions.None).LastOrDefault().Split(':', StringSplitOptions.None);
                        string item = source.FirstOrDefault();
                        int num = Convert.ToInt32(source.LastOrDefault());
                        List<string> list = new List<string>();
                        list.AddRange(getIps());
                        if (!list.Contains(item))
                        {
                            list.Add(item);
                        }
                        if (num > 0)
                        {
                            foreach (string item2 in list)
                            {
                                AgentServiceRegistration agentServiceRegistration = new AgentServiceRegistration();
                                agentServiceRegistration.ID = $"{serviceConfig.SERVICE_NAME}:{item2}:{num}";
                                agentServiceRegistration.Name = serviceConfig.SERVICE_NAME;
                                agentServiceRegistration.Address = item2;
                                agentServiceRegistration.Port = num;
                                agentServiceRegistration.Tags = new string[3]
                                {
                                    serviceConfig.SERVICE_TAGS,
                                    hosting.EnvironmentName,
                                    hosting.ApplicationName
                                };
                                agentServiceRegistration.EnableTagOverride = true;
                                AgentServiceRegistration agentServiceRegistration2 = agentServiceRegistration;
                                List<AgentServiceCheck> list2 = new List<AgentServiceCheck>();
                                if (!string.IsNullOrEmpty(serviceConfig.SERVICE_80_CHECK_HTTP))
                                {
                                    list2.Add(new AgentServiceCheck
                                    {
                                        Status = HealthStatus.Critical,
                                        HTTP = $"http://{item2}:{num}/{serviceConfig.SERVICE_80_CHECK_HTTP.TrimStart('/')}",
                                        Interval = new TimeSpan?(TimeSpan.FromSeconds((double)int.Parse(serviceConfig.SERVICE_80_CHECK_INTERVAL.TrimEnd('s')))),
                                        Timeout = new TimeSpan?(TimeSpan.FromSeconds((double)int.Parse(serviceConfig.SERVICE_80_CHECK_TIMEOUT.TrimEnd('s')))),
                                        DeregisterCriticalServiceAfter = TimeSpan.FromDays(7)
                                    });
                                }
                                else if (!string.IsNullOrEmpty(serviceConfig.SERVICE_CHECK_TCP))
                                {
                                    list2.Add(new AgentServiceCheck
                                    {
                                        Status = HealthStatus.Critical,
                                        TCP = serviceConfig.SERVICE_CHECK_TCP,
                                        Interval = new TimeSpan?(TimeSpan.FromSeconds((double)int.Parse(serviceConfig.SERVICE_CHECK_INTERVAL.TrimEnd('s')))),
                                        Timeout = new TimeSpan?(TimeSpan.FromSeconds((double)int.Parse(serviceConfig.SERVICE_CHECK_TIMEOUT.TrimEnd('s')))),
                                        DeregisterCriticalServiceAfter = TimeSpan.FromDays(7)
                                    });
                                }
                                else if (!string.IsNullOrEmpty(serviceConfig.SERVICE_CHECK_SCRIPT))
                                {
                                    list2.Add(new AgentServiceCheck
                                    {
                                        Status = HealthStatus.Critical,
                                        Script = serviceConfig.SERVICE_CHECK_SCRIPT,
                                        Interval = new TimeSpan?(TimeSpan.FromSeconds((double)int.Parse(serviceConfig.SERVICE_CHECK_INTERVAL.TrimEnd('s')))),
                                        Timeout = new TimeSpan?(TimeSpan.FromSeconds((double)int.Parse(serviceConfig.SERVICE_CHECK_TIMEOUT.TrimEnd('s')))),
                                        DeregisterCriticalServiceAfter = TimeSpan.FromDays(7)
                                    });
                                }
                                else if (serviceConfig.SERVICE_CHECK_TTL.HasValue)
                                {
                                    list2.Add(new AgentServiceCheck
                                    {
                                        Status = HealthStatus.Critical,
                                        TTL = new TimeSpan?(TimeSpan.FromSeconds((double)serviceConfig.SERVICE_CHECK_TTL.Value))
                                    });
                                }
                                agentServiceRegistration2.Checks = list2.ToArray();
                                registrations.Add(agentServiceRegistration2);
                            }
                        }
                        else
                        {
                            logger.LogWarning("No registration service. port invalid", Array.Empty<object>());
                        }
                    }
                    else
                    {
                        AgentServiceRegistration agentServiceRegistration = new AgentServiceRegistration();
                        agentServiceRegistration.ID = (serviceConfig.SERVICE_NAME ?? "");
                        agentServiceRegistration.Name = serviceConfig.SERVICE_NAME;
                        agentServiceRegistration.Tags = new string[3]
                        {
                            serviceConfig.SERVICE_TAGS,
                            hosting.EnvironmentName,
                            hosting.ApplicationName
                        };
                        agentServiceRegistration.EnableTagOverride = true;
                        AgentServiceRegistration agentServiceRegistration3 = agentServiceRegistration;
                        List<AgentServiceCheck> list3 = new List<AgentServiceCheck>();
                        if (!string.IsNullOrEmpty(serviceConfig.SERVICE_CHECK_TCP))
                        {
                            list3.Add(new AgentServiceCheck
                            {
                                Status = HealthStatus.Critical,
                                TCP = serviceConfig.SERVICE_CHECK_TCP,
                                Interval = new TimeSpan?(TimeSpan.FromSeconds((double)int.Parse(serviceConfig.SERVICE_CHECK_INTERVAL.TrimEnd('s')))),
                                Timeout = new TimeSpan?(TimeSpan.FromSeconds((double)int.Parse(serviceConfig.SERVICE_CHECK_TIMEOUT.TrimEnd('s')))),
                                DeregisterCriticalServiceAfter = TimeSpan.FromDays(7)
                            });
                        }
                        else if (!string.IsNullOrEmpty(serviceConfig.SERVICE_CHECK_SCRIPT))
                        {
                            list3.Add(new AgentServiceCheck
                            {
                                Status = HealthStatus.Critical,
                                Script = serviceConfig.SERVICE_CHECK_SCRIPT,
                                Interval = new TimeSpan?(TimeSpan.FromSeconds((double)int.Parse(serviceConfig.SERVICE_CHECK_INTERVAL.TrimEnd('s')))),
                                Timeout = new TimeSpan?(TimeSpan.FromSeconds((double)int.Parse(serviceConfig.SERVICE_CHECK_TIMEOUT.TrimEnd('s')))),
                                DeregisterCriticalServiceAfter = TimeSpan.FromDays(7)
                            });
                        }
                        else if (serviceConfig.SERVICE_CHECK_TTL.HasValue)
                        {
                            list3.Add(new AgentServiceCheck
                            {
                                Status = HealthStatus.Critical,
                                TTL = new TimeSpan?(TimeSpan.FromSeconds((double)serviceConfig.SERVICE_CHECK_TTL.Value))
                            });
                        }
                        agentServiceRegistration3.Checks = list3.ToArray();
                        registrations.Add(agentServiceRegistration3);
                    }
                    CancellationToken cancellationToken = lifetime.ApplicationStarted;
                    cancellationToken.Register(delegate
                    {
                        foreach (AgentServiceRegistration item3 in registrations)
                        {
                            policy.Execute((Func<Task>)async delegate
                            {
                                logger.LogInformation("service " + item3.ID + " registration", Array.Empty<object>());
                                WriteResult ret3 = await client.Agent.ServiceRegister(item3, default(CancellationToken));
                                logger.LogInformation($"service {item3.ID} registered. time={ret3.RequestTime},statusCode={ret3.StatusCode}", Array.Empty<object>());
                            });
                        }
                        if (serviceConfig.SERVICE_CHECK_TTL.HasValue && !string.IsNullOrEmpty(serviceConfig.SERVICE_CHECK_INTERVAL))
                        {
                            try
                            {
                                var timer = new System.Timers.Timer((double)(int.Parse(serviceConfig.SERVICE_CHECK_INTERVAL) * 1000));
                                timer.Elapsed += async delegate
                                {
                                    List<AgentServiceRegistration>.Enumerator enumerator5 = registrations.GetEnumerator();
                                    try
                                    {
                                        while (enumerator5.MoveNext())
                                        {
                                            AgentServiceRegistration registration4 = enumerator5.Current;
                                            try
                                            {
                                                await client.Agent.PassTTL("service:" + registration4.ID, "", default(CancellationToken));
                                                logger.LogDebug("service " + registration4.ID + " ttl passing", Array.Empty<object>());
                                            }
                                            catch
                                            {
                                            }
                                        }
                                    }
                                    finally
                                    {
                                        ((IDisposable)enumerator5).Dispose();
                                    }
                                    enumerator5 = default(List<AgentServiceRegistration>.Enumerator);
                                };
                                timer.Start();
                            }
                            catch
                            {
                            }
                        }
                    });
                    cancellationToken = lifetime.ApplicationStopping;
                    cancellationToken.Register(delegate
                    {
                        try
                        {
                            foreach (AgentServiceRegistration item4 in registrations)
                            {
                                policy.Execute(async delegate
                                {
                                    logger.LogInformation("service " + item4.ID + " deregister", Array.Empty<object>());
                                    WriteResult ret2 = await client.Agent.ServiceDeregister(item4.ID, default(CancellationToken));
                                    logger.LogInformation($"service {item4.ID} Deregistered. time={ret2.RequestTime},statusCode={ret2.StatusCode}", Array.Empty<object>());
                                    return Task.FromResult(true);
                                });
                            }
                        }
                        catch (Exception)
                        {
                        }
                    });
                    AppDomain.CurrentDomain.ProcessExit += delegate
                    {
                        foreach (AgentServiceRegistration item5 in registrations)
                        {
                            policy.Execute(async delegate
                            {
                                logger.LogInformation("service " + item5.ID + " deregister", Array.Empty<object>());
                                WriteResult ret = await client.Agent.ServiceDeregister(item5.ID, default(CancellationToken));
                                logger.LogInformation($"service {item5.ID} Deregistered. time={ret.RequestTime},statusCode={ret.StatusCode}", Array.Empty<object>());
                                return Task.FromResult(true);
                            });
                        }
                    };
                }
                else
                {
                    logger.LogWarning("No registration service", Array.Empty<object>());
                }
            }
            catch (Exception ex2)
            {
                logger.LogError(ex2, ex2.Message, Array.Empty<object>());
            }
        }
    }

}
