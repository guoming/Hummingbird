using Consul;
using Hummingbird.DynamicRoute;
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
    class ConsulServiceDiscoveryProvider : IServiceDiscoveryProvider
    {
        private readonly IHealthCheckService _healthCheckService;
        private readonly ILogger<ConsulServiceDiscoveryProvider> _logger;
        private readonly IHostingEnvironment _hostingEnvironment;
        private readonly IConfiguration _configuration;
        private readonly ConsulConfig _serviceConfig = new ConsulConfig();
        private readonly IConsulClient _client;
        private readonly List<AgentServiceRegistration> _registrations;
        /**获取ip地址*/
        private List<string> getIps()
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
        private void LogDebug(string message, params object[] objs)
        {
            _logger?.LogDebug(message, objs);
        }

        private void LogWarning(Exception exception, string message, params object[] objs)
        {
            _logger?.LogWarning(exception, message, objs);
        }

        private void LogWarning(string message)
        {
            _logger?.LogWarning(message);
        }

        private void LogError(Exception exception, string message, params object[] objs)
        {
            _logger?.LogError(exception, message, objs);
        }

        private void LogInformation(string message, params object[] objs)
        {
            _logger?.LogInformation(message, objs);
        }
        #endregion

        public ConsulServiceDiscoveryProvider(
            IConsulClient client,
            IHealthCheckService healthCheckService,
            ILogger<ConsulServiceDiscoveryProvider> logger,
            IHostingEnvironment hostingEnvironment,
            IConfiguration configuration,      
            ConsulConfig consulConfig)
        {
            this._client =client;
            this._healthCheckService = healthCheckService;
            this._logger = logger;
            this._hostingEnvironment = hostingEnvironment;
            this._configuration = configuration;
            this._serviceConfig = consulConfig;
            this._registrations = new List<AgentServiceRegistration>();
                
            var urls = string.Empty;
            var tags = new List<string>();

            if (configuration != null)
            {
                urls = configuration["urls"]?.TrimEnd('/');
            }

            if (hostingEnvironment != null)
            {
                if (!string.IsNullOrEmpty(hostingEnvironment.EnvironmentName))
                {
                    tags.Add(hostingEnvironment.EnvironmentName);
                }

                if (!string.IsNullOrEmpty(hostingEnvironment.ApplicationName))
                {
                    tags.Add(hostingEnvironment.ApplicationName);
                }
            }

            try
            {
                if (!string.IsNullOrEmpty(_serviceConfig.SERVICE_TAGS))
                {
                    tags.AddRange(_serviceConfig.SERVICE_TAGS.Split(','));
                }
            
                if (string.IsNullOrEmpty(_serviceConfig.SERVICE_SELF_REGISTER) || _serviceConfig.SERVICE_SELF_REGISTER.ToLower() == bool.TrueString.ToString().ToLower())
                {
                    if (!string.IsNullOrEmpty(urls) && urls.Contains("/") && urls.Contains(":"))
                    {
                        string[] source = urls.Split('/').LastOrDefault().Split(':');
                        string ip = source.FirstOrDefault();
                        int port = Convert.ToInt32(source.LastOrDefault());
                        List<string> ipList = new List<string>();

                        if (ip == "0.0.0.0" || ip == "*")
                        {
                            ipList.AddRange(getIps());
                        }
                        else if (!ipList.Contains(ip))
                        {
                            ipList.Add(ip);
                        }

                        if (port > 0)
                        {
                            foreach (string item2 in ipList)
                            {
                                AgentServiceRegistration agentServiceRegistration = new AgentServiceRegistration();
                                agentServiceRegistration.ID = string.IsNullOrEmpty(_serviceConfig.SERVICE_ID) ? $"{_serviceConfig.SERVICE_NAME}:{item2}:{port}" : $"{_serviceConfig.SERVICE_NAME}:{_serviceConfig.SERVICE_ID}";
                                agentServiceRegistration.Name = _serviceConfig.SERVICE_NAME;
                                agentServiceRegistration.Address = item2;
                                agentServiceRegistration.Port = port;
                                agentServiceRegistration.Tags = tags.ToArray();
                                agentServiceRegistration.EnableTagOverride = true;
                                agentServiceRegistration.Checks = GetChecks(item2, port, TimeSpan.FromDays(7)).ToArray();
                                _registrations.Add(agentServiceRegistration);
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
                        agentServiceRegistration.ID = string.IsNullOrEmpty(_serviceConfig.SERVICE_ID) ? $"{_serviceConfig.SERVICE_NAME}:{Guid.NewGuid().ToString()}" : $"{_serviceConfig.SERVICE_NAME}:{_serviceConfig.SERVICE_ID}";
                        agentServiceRegistration.Name = _serviceConfig.SERVICE_NAME;
                        agentServiceRegistration.Tags = tags.ToArray();
                        agentServiceRegistration.EnableTagOverride = true;                        
                        agentServiceRegistration.Checks = GetChecks("", 0, TimeSpan.FromSeconds(10 * double.Parse(_serviceConfig.SERVICE_CHECK_TIMEOUT.TrimEnd('s')))).ToArray();
                        _registrations.Add(agentServiceRegistration);

                    }


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

        private List<AgentServiceCheck> GetChecks(string ip, int port, TimeSpan DeregisterCriticalServiceAfter)
        {
            List<AgentServiceCheck> agentServiceChecks = new List<AgentServiceCheck>();

            if (!string.IsNullOrEmpty(_serviceConfig.SERVICE_80_CHECK_HTTP) && port > 0 && !string.IsNullOrEmpty(ip))
            {
                agentServiceChecks.Add(new AgentServiceCheck
                {
                    Status = HealthStatus.Critical,
                    HTTP = $"http://{ip}:{port}/{_serviceConfig.SERVICE_80_CHECK_HTTP.TrimStart('/')}",
                    Interval = new TimeSpan?(TimeSpan.FromSeconds((double)int.Parse(_serviceConfig.SERVICE_80_CHECK_INTERVAL.TrimEnd('s')))),
                    Timeout = new TimeSpan?(TimeSpan.FromSeconds((double)int.Parse(_serviceConfig.SERVICE_80_CHECK_TIMEOUT.TrimEnd('s')))),
                    DeregisterCriticalServiceAfter = DeregisterCriticalServiceAfter
                });
            }
            else if (!string.IsNullOrEmpty(_serviceConfig.SERVICE_CHECK_TCP))
            {
                agentServiceChecks.Add(new AgentServiceCheck
                {
                    Status = HealthStatus.Critical,
                    TCP = _serviceConfig.SERVICE_CHECK_TCP,
                    Interval = new TimeSpan?(TimeSpan.FromSeconds((double)int.Parse(_serviceConfig.SERVICE_CHECK_INTERVAL.TrimEnd('s')))),
                    Timeout = new TimeSpan?(TimeSpan.FromSeconds((double)int.Parse(_serviceConfig.SERVICE_CHECK_TIMEOUT.TrimEnd('s')))),
                    DeregisterCriticalServiceAfter = DeregisterCriticalServiceAfter
                });
            }
            else if (!string.IsNullOrEmpty(_serviceConfig.SERVICE_CHECK_SCRIPT))
            {
                agentServiceChecks.Add(new AgentServiceCheck
                {
                    Status = HealthStatus.Critical,
                    Script = _serviceConfig.SERVICE_CHECK_SCRIPT,
                    Interval = new TimeSpan?(TimeSpan.FromSeconds((double)int.Parse(_serviceConfig.SERVICE_CHECK_INTERVAL.TrimEnd('s')))),
                    Timeout = new TimeSpan?(TimeSpan.FromSeconds((double)int.Parse(_serviceConfig.SERVICE_CHECK_TIMEOUT.TrimEnd('s')))),
                    DeregisterCriticalServiceAfter = DeregisterCriticalServiceAfter
                });
            }
            else if (_serviceConfig.SERVICE_CHECK_TTL.HasValue)
            {
                agentServiceChecks.Add(new AgentServiceCheck
                {
                    Status = HealthStatus.Critical,
                    TTL = new TimeSpan?(TimeSpan.FromSeconds((double)_serviceConfig.SERVICE_CHECK_TTL.Value)),
                    DeregisterCriticalServiceAfter = DeregisterCriticalServiceAfter                    
                });
            }

            return agentServiceChecks;
        }

        private void Heartbeat(List<AgentServiceRegistration> registrations)
        {
            if (_serviceConfig.SERVICE_CHECK_TTL.HasValue && !string.IsNullOrEmpty(_serviceConfig.SERVICE_CHECK_INTERVAL))
            {
                try
                {
                    var timer = new System.Timers.Timer((double)(int.Parse(_serviceConfig.SERVICE_CHECK_INTERVAL) * 1000));
                    timer.Elapsed += async delegate
                    {
                        try
                        {
                            var result = await _healthCheckService.CheckHealthAsync();
                            var status = result.CheckStatus;

                            if (status == CheckStatus.Healthy)
                            {
                                foreach (var registration in registrations)
                                {
                                    try
                                    {
                                        await _client.Agent.PassTTL("service:" + registration.ID, result.Description, default(CancellationToken));
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
                                        await _client.Agent.WarnTTL("service:" + registration.ID, result.Description, default(CancellationToken));
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
                                        await _client.Agent.FailTTL("service:" + registration.ID, result.Description, default(CancellationToken));
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
                catch (Exception ex)
                {
                    LogError(ex, ex.Message);
                }
            }
        }

        public void Register()
        {
            var policy = Policy.Handle<Exception>().Or<IOException>().WaitAndRetryForever((int a) => TimeSpan.FromSeconds(5.0), delegate (Exception ex, TimeSpan time)
            {
                _logger.LogError(ex, ex.Message, Array.Empty<object>());
            });

            foreach (AgentServiceRegistration item3 in _registrations)
            {
                policy.Execute((Func<Task>)async delegate
                {
                    LogInformation("service " + item3.ID + " registration", Array.Empty<object>());
                    WriteResult ret3 = await _client.Agent.ServiceRegister(item3, default(CancellationToken));                    
                    LogInformation($"service {item3.ID} registered. time={ret3.RequestTime},statusCode={ret3.StatusCode}", Array.Empty<object>());
                });
            }

            Heartbeat(_registrations);
        }

        public void Deregister()
        {
            var policy = Policy.Handle<Exception>().Or<IOException>().WaitAndRetryForever((int a) => TimeSpan.FromSeconds(5.0), delegate (Exception ex, TimeSpan time)
            {
                _logger.LogError(ex, ex.Message, Array.Empty<object>());
            });

            foreach (AgentServiceRegistration item4 in _registrations)
            {
                policy.Execute(async delegate
                {
                    _logger.LogInformation("service " + item4.ID + " deregister", Array.Empty<object>());
                    WriteResult ret2 = await _client.Agent.ServiceDeregister(item4.ID, default(CancellationToken));
                    _logger.LogInformation($"service {item4.ID} Deregistered. time={ret2.RequestTime},statusCode={ret2.StatusCode}", Array.Empty<object>());

                });
            }

        }

        public string ServiceId
        {
            get
            {
                if (_registrations.Any())
                {
                    return string.Join(",", _registrations.Select(a => a.ID).ToList());
                }
                else
                { return string.Empty; }
            }
        }
    }


}
