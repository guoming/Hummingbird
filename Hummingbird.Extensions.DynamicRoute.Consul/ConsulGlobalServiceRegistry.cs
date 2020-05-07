using Consul;
using Hummingbird.Extensions.HealthChecks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Polly;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Hummingbird.Extensions.DynamicRoute.Consul
{
    internal static class ConsulGlobalServiceRegistry
    {
        private static List<AgentServiceRegistration> registrations = new List<AgentServiceRegistration>();
        private static ILogger<ConsulClient> logger;
        private static IHealthCheckService healthCheckService;
        private static ConsulConfig serviceConfig = new ConsulConfig();
        private static ConsulClient client;

        /**获取ip地址*/
        private static List<string> getIps()
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


        #region 日志
        private static void LogDebug(string message, params object[] objs)
        {
            logger?.LogDebug( message, objs);
        }

        private static void LogWarning(Exception exception, string message, params object[] objs)
        {
            logger?.LogWarning(exception, message, objs);
        }

        private static void LogWarning(string message)
        {
            logger?.LogWarning(message);
        }

        private static void LogError(Exception exception, string message, params object[] objs)
        {
            logger?.LogError(exception, message, objs);
        }

        private static void LogInformation(string message, params object[] objs)
        {
            logger?.LogInformation(message, objs);
        }
        #endregion


        public static void Build(IServiceProvider serviceProvider, Action<ConsulConfig> setup)
        {
            healthCheckService = ServiceProviderServiceExtensions.GetService<IHealthCheckService>(serviceProvider);
            var hosting = ServiceProviderServiceExtensions.GetService<IHostingEnvironment>(serviceProvider);
            var configuration = ServiceProviderServiceExtensions.GetService<IConfiguration>(serviceProvider);

            logger = ServiceProviderServiceExtensions.GetService<ILogger<ConsulClient>>(serviceProvider);

            var urls = string.Empty;
            var tags = new List<string>();

            if(configuration!=null)
            {
                urls = configuration["urls"]?.TrimEnd('/');
            }

            if (hosting != null)
            {
                if (!string.IsNullOrEmpty(hosting.EnvironmentName))
                {
                    tags.Add(hosting.EnvironmentName);
                }

                if (!string.IsNullOrEmpty(hosting.ApplicationName))
                {
                    tags.Add(hosting.ApplicationName);
                }
            }

            try
            {
                setup?.Invoke(serviceConfig);

                if (!string.IsNullOrEmpty(serviceConfig.SERVICE_TAGS))
                {
                    tags.AddRange(serviceConfig.SERVICE_TAGS.Split(','));
                }

                client = new ConsulClient(delegate (ConsulClientConfiguration obj)
                {
                    obj.Address = new Uri("http://" + serviceConfig.SERVICE_REGISTRY_ADDRESS + ":" + serviceConfig.SERVICE_REGISTRY_PORT);
                    obj.Datacenter = serviceConfig.SERVICE_REGION;
                    obj.Token = serviceConfig.SERVICE_REGISTRY_TOKEN;
                });

                if (string.IsNullOrEmpty(serviceConfig.SERVICE_SELF_REGISTER) || serviceConfig.SERVICE_SELF_REGISTER.ToLower() == bool.TrueString.ToString().ToLower())
                {
                    if (!string.IsNullOrEmpty(urls) && urls.Contains("/") && urls.Contains(":"))
                    {
                        string[] source = urls.Split('/').LastOrDefault().Split(':');
                        string ip = source.FirstOrDefault();
                        int port = Convert.ToInt32(source.LastOrDefault());
                        List<string> ipList = new List<string>();

                        if (ip == "0.0.0.0")
                        {
                            ipList.AddRange(getIps());
                        }

                        if (!ipList.Contains(ip))
                        {
                            ipList.Add(ip);
                        }

                        if (port > 0)
                        {
                            foreach (string item2 in ipList)
                            {
                                AgentServiceRegistration agentServiceRegistration = new AgentServiceRegistration();
                                agentServiceRegistration.ID = string.IsNullOrEmpty(serviceConfig.SERVICE_ID) ? $"{serviceConfig.SERVICE_NAME}:{item2}:{port}" : $"{serviceConfig.SERVICE_NAME}:{serviceConfig.SERVICE_ID}";
                                agentServiceRegistration.Name = serviceConfig.SERVICE_NAME;
                                agentServiceRegistration.Address = item2;
                                agentServiceRegistration.Port = port;   
                                agentServiceRegistration.Tags = tags.ToArray();
                                agentServiceRegistration.EnableTagOverride = true;
                                agentServiceRegistration.Checks = GetChecks(item2, port,TimeSpan.FromDays(7)).ToArray();
                                registrations.Add(agentServiceRegistration);
                            }
                        }
                        else
                        {
                            LogWarning("No registration service. port invalid");
                        }
                    }
                    else
                    {
                        AgentServiceRegistration agentServiceRegistration = new AgentServiceRegistration();
                        agentServiceRegistration.ID = string.IsNullOrEmpty(serviceConfig.SERVICE_ID) ? $"{serviceConfig.SERVICE_NAME}:{Guid.NewGuid().ToString()}" : $"{serviceConfig.SERVICE_NAME}:{serviceConfig.SERVICE_ID}";
                        agentServiceRegistration.Name = serviceConfig.SERVICE_NAME;                       
                        agentServiceRegistration.Tags = tags.ToArray();
                        agentServiceRegistration.EnableTagOverride = true;
                        agentServiceRegistration.Checks = GetChecks("", 0, TimeSpan.FromSeconds(10 * double.Parse(serviceConfig.SERVICE_CHECK_TIMEOUT.TrimEnd('s')))).ToArray();
                        registrations.Add(agentServiceRegistration);
                    }
              

                    AppDomain.CurrentDomain.ProcessExit += delegate
                    {
                        Deregister();
                    };
                }
                else
                {
                    LogWarning("No registration service");
                }
            }
            catch (Exception ex2)
            {
                LogError(ex2, ex2.Message, Array.Empty<object>());
            }
        }

        private static List<AgentServiceCheck> GetChecks(string ip,int port, TimeSpan DeregisterCriticalServiceAfter)
        {
            List<AgentServiceCheck> agentServiceChecks = new List<AgentServiceCheck>();

            if (!string.IsNullOrEmpty(serviceConfig.SERVICE_80_CHECK_HTTP) && port>0 && !string.IsNullOrEmpty(ip))
            {
                agentServiceChecks.Add(new AgentServiceCheck
                {
                    Status = HealthStatus.Critical,
                    HTTP = $"http://{ip}:{port}/{serviceConfig.SERVICE_80_CHECK_HTTP.TrimStart('/')}",
                    Interval = new TimeSpan?(TimeSpan.FromSeconds((double)int.Parse(serviceConfig.SERVICE_80_CHECK_INTERVAL.TrimEnd('s')))),
                    Timeout = new TimeSpan?(TimeSpan.FromSeconds((double)int.Parse(serviceConfig.SERVICE_80_CHECK_TIMEOUT.TrimEnd('s')))),
                    DeregisterCriticalServiceAfter = DeregisterCriticalServiceAfter
                });
            }
            else if (!string.IsNullOrEmpty(serviceConfig.SERVICE_CHECK_TCP))
            {
                agentServiceChecks.Add(new AgentServiceCheck
                {
                    Status = HealthStatus.Critical,
                    TCP = serviceConfig.SERVICE_CHECK_TCP,
                    Interval = new TimeSpan?(TimeSpan.FromSeconds((double)int.Parse(serviceConfig.SERVICE_CHECK_INTERVAL.TrimEnd('s')))),
                    Timeout = new TimeSpan?(TimeSpan.FromSeconds((double)int.Parse(serviceConfig.SERVICE_CHECK_TIMEOUT.TrimEnd('s')))),
                    DeregisterCriticalServiceAfter = DeregisterCriticalServiceAfter
                });
            }
            else if (!string.IsNullOrEmpty(serviceConfig.SERVICE_CHECK_SCRIPT))
            {
                agentServiceChecks.Add(new AgentServiceCheck
                {
                    Status = HealthStatus.Critical,
                    Script = serviceConfig.SERVICE_CHECK_SCRIPT,
                    Interval = new TimeSpan?(TimeSpan.FromSeconds((double)int.Parse(serviceConfig.SERVICE_CHECK_INTERVAL.TrimEnd('s')))),
                    Timeout = new TimeSpan?(TimeSpan.FromSeconds((double)int.Parse(serviceConfig.SERVICE_CHECK_TIMEOUT.TrimEnd('s')))),
                    DeregisterCriticalServiceAfter = DeregisterCriticalServiceAfter
                });
            }
            else if (serviceConfig.SERVICE_CHECK_TTL.HasValue)
            {
                agentServiceChecks.Add(new AgentServiceCheck
                {
                    Status = HealthStatus.Critical,
                    TTL = new TimeSpan?(TimeSpan.FromSeconds((double)serviceConfig.SERVICE_CHECK_TTL.Value)),
                    DeregisterCriticalServiceAfter = DeregisterCriticalServiceAfter
                });
            }

            return agentServiceChecks;
        }

        private static void Heartbeat()
        {
            if (serviceConfig.SERVICE_CHECK_TTL.HasValue && !string.IsNullOrEmpty(serviceConfig.SERVICE_CHECK_INTERVAL))
            {
                try
                {
                    var timer = new System.Timers.Timer((double)(int.Parse(serviceConfig.SERVICE_CHECK_INTERVAL) * 1000));
                    timer.Elapsed += async delegate
                    {
                        try
                        {
                            var result = await healthCheckService.CheckHealthAsync();
                            var status = result.CheckStatus;

                            if (status == CheckStatus.Healthy)
                            {
                                foreach (var registration in registrations)
                                {
                                    try
                                    {
                                        await client.Agent.PassTTL("service:" + registration.ID, result.Description, default(CancellationToken));
                                        LogDebug("service " + registration.ID + " ttl passing", Array.Empty<object>());
                                    }
                                    catch (Exception ex)
                                    {

                                        LogWarning(ex, ex.Message);
                                    }
                                }
                            }
                            else if (status == CheckStatus.Warning)
                            {
                                foreach (var registration in registrations)
                                {

                                    try
                                    {
                                        await client.Agent.WarnTTL("service:" + registration.ID, result.Description, default(CancellationToken));
                                        LogDebug("service " + registration.ID + " ttl warn", Array.Empty<object>());
                                    }
                                    catch (Exception ex)
                                    {

                                        LogWarning(ex, ex.Message);
                                    }
                                }
                            }
                            else
                            {
                                foreach (var registration in registrations)
                                {

                                    try
                                    {
                                        await client.Agent.FailTTL("service:" + registration.ID, result.Description, default(CancellationToken));
                                        LogDebug("service " + registration.ID + " ttl failed", Array.Empty<object>());
                                    }
                                    catch (Exception ex)
                                    {
                                        LogWarning(ex, ex.Message);
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            LogError(ex, ex.Message);
                        }
                    };
                    timer.Start();
                }
                catch(Exception ex)
                {
                    LogError(ex, ex.Message);
                }
            }
        }

        public static void Register()
        {
            var policy = Policy.Handle<Exception>().Or<IOException>().WaitAndRetryForever((int a) => TimeSpan.FromSeconds(5.0), delegate (Exception ex, TimeSpan time)
            {
                logger.LogError(ex, ex.Message, Array.Empty<object>());
            });

            foreach (AgentServiceRegistration item3 in registrations)
            {
                policy.Execute((Func<Task>)async delegate
                {
                    LogInformation("service " + item3.ID + " registration", Array.Empty<object>());
                    WriteResult ret3 = await client.Agent.ServiceRegister(item3, default(CancellationToken));
                    LogInformation($"service {item3.ID} registered. time={ret3.RequestTime},statusCode={ret3.StatusCode}", Array.Empty<object>());
                });
            }

            Heartbeat();
        }

        public static void Deregister()
        {
            var policy = Policy.Handle<Exception>().Or<IOException>().WaitAndRetryForever((int a) => TimeSpan.FromSeconds(5.0), delegate (Exception ex, TimeSpan time)
            {
                logger.LogError(ex, ex.Message, Array.Empty<object>());
            });
          
            foreach (AgentServiceRegistration item4 in registrations)
            {
                policy.Execute(async delegate
                {
                    logger.LogInformation("service " + item4.ID + " deregister", Array.Empty<object>());
                    WriteResult ret2 = await client.Agent.ServiceDeregister(item4.ID, default(CancellationToken));
                    logger.LogInformation($"service {item4.ID} Deregistered. time={ret2.RequestTime},statusCode={ret2.StatusCode}", Array.Empty<object>());
                  
                });
            }
           
        }
    }
}
