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
        private bool registerCompleted = false;

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
                        var ipList = new List<string>();
                        var endpoints = urls.Split(new string[] {";", ",", " " }, StringSplitOptions.RemoveEmptyEntries);

                        foreach(var url in endpoints)
                        {
                            var uri = new Uri(url);

                            if (uri.Host == "0.0.0.0" || uri.Host == "*")
                            {
                                ipList.AddRange(getIps().Where(a=>a!="127.0.0.1"));
                            }
                            else if (!ipList.Contains(uri.Host))
                            {
                                ipList.Add(uri.Host);
                            }                                               

                            if (uri.Port > 0)
                            {
                                foreach (string item2 in ipList)
                                {
                                    var checks = new List<AgentServiceCheck>();
                                    checks.AddRange(GetHTTPChecks(uri.Scheme, item2, uri.Port));
                                   
                                    AgentServiceRegistration agentServiceRegistration = new AgentServiceRegistration();
                                    agentServiceRegistration.ID = string.IsNullOrEmpty(_serviceConfig.SERVICE_ID) ? $"{_serviceConfig.SERVICE_NAME}:{item2}:{uri.Port}" : $"{_serviceConfig.SERVICE_NAME}:{_serviceConfig.SERVICE_ID}";
                                    agentServiceRegistration.Name = _serviceConfig.SERVICE_NAME;
                                    agentServiceRegistration.Address = item2;
                                    agentServiceRegistration.Port = uri.Port;
                                    agentServiceRegistration.Tags = tags.ToArray();
                                    agentServiceRegistration.EnableTagOverride = true;
                                    agentServiceRegistration.Checks = checks.ToArray();
                                    _registrations.Add(agentServiceRegistration);
                                }
                            }
                            else
                            {
                                LogWarning("No registration service. port invalid");
                            }
                        }                       
                    }
                    else
                    {
                        AgentServiceRegistration agentServiceRegistration = new AgentServiceRegistration();
                        agentServiceRegistration.ID = string.IsNullOrEmpty(_serviceConfig.SERVICE_ID) ? $"{_serviceConfig.SERVICE_NAME}:{Guid.NewGuid().ToString()}" : $"{_serviceConfig.SERVICE_NAME}:{_serviceConfig.SERVICE_ID}";
                        agentServiceRegistration.Name = _serviceConfig.SERVICE_NAME;
                        agentServiceRegistration.Tags = tags.ToArray();
                        agentServiceRegistration.EnableTagOverride = true;                        
                        agentServiceRegistration.Checks = GetChecksWithoutHttp().ToArray();
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

        private List<AgentServiceCheck> GetHTTPChecks(string schema, string ip, int port)
        {
            List<AgentServiceCheck> agentServiceChecks = new List<AgentServiceCheck>();

            if (!string.IsNullOrEmpty(_serviceConfig.SERVICE_CHECK_HTTP) && port > 0 && !string.IsNullOrEmpty(ip))
            {
                var interval = int.Parse(_serviceConfig.SERVICE_CHECK_INTERVAL.TrimEnd('s'));
                var timeout = int.Parse(_serviceConfig.SERVICE_CHECK_TIMEOUT.TrimEnd('s'));

                agentServiceChecks.Add(new AgentServiceCheck
                {
                    Status = HealthStatus.Critical,
                    HTTP = $"{schema}://{ip}:{port}/{_serviceConfig.SERVICE_CHECK_HTTP.TrimStart('/')}",
                    Interval = TimeSpan.FromSeconds(interval),
                    Timeout = TimeSpan.FromSeconds(timeout),
                    DeregisterCriticalServiceAfter = TimeSpan.FromDays(7)
                });
            }

            return agentServiceChecks;
        }

        private List<AgentServiceCheck> GetChecksWithoutHttp()
        {
            List<AgentServiceCheck> agentServiceChecks = new List<AgentServiceCheck>();
            var interval = int.Parse(_serviceConfig.SERVICE_CHECK_INTERVAL.TrimEnd('s'));
            var timeout = int.Parse(_serviceConfig.SERVICE_CHECK_TIMEOUT.TrimEnd('s'));
            var ttl = _serviceConfig.SERVICE_CHECK_TTL.HasValue?_serviceConfig.SERVICE_CHECK_TTL.Value: interval*3;

       
            if (!string.IsNullOrEmpty(_serviceConfig.SERVICE_CHECK_TCP))
            {
                agentServiceChecks.Add(new AgentServiceCheck
                {
                    Status = HealthStatus.Critical,
                    TCP = _serviceConfig.SERVICE_CHECK_TCP,
                    Interval = TimeSpan.FromSeconds(interval),
                    Timeout = TimeSpan.FromSeconds(timeout),
                    DeregisterCriticalServiceAfter = TimeSpan.FromDays(7)
                });
            }
            else if (!string.IsNullOrEmpty(_serviceConfig.SERVICE_CHECK_SCRIPT))
            {
                agentServiceChecks.Add(new AgentServiceCheck
                {
                    Status = HealthStatus.Critical,
                    Script = _serviceConfig.SERVICE_CHECK_SCRIPT,
                    Interval = TimeSpan.FromSeconds(interval),
                    Timeout = TimeSpan.FromSeconds(timeout),
                    DeregisterCriticalServiceAfter = TimeSpan.FromDays(7)
                });
            }
            else if (_serviceConfig.SERVICE_CHECK_TTL.HasValue)
            {
                agentServiceChecks.Add(new AgentServiceCheck
                {
                    Status = HealthStatus.Critical,
                    TTL =TimeSpan.FromSeconds(ttl),
                    DeregisterCriticalServiceAfter = TimeSpan.FromDays(7)
                });
            }

            return agentServiceChecks;
        }

        public async void Heartbeat()
        {
            if (registerCompleted)
            {

                var result = await _healthCheckService.CheckHealthAsync();
                var status = result.CheckStatus;

                
                try
                {

                    foreach (var registration in _registrations)
                    {
                        if (registration.Checks.Length > 1)
                        {
                            for (int i = 0; i < registration.Checks.Length; i++)
                            {
                                if (registration.Checks[i].TTL.HasValue)
                                {
                                    try
                                    {
                                        if (status == CheckStatus.Healthy)
                                        {

                                            await _client.Agent.PassTTL($"service:{registration.ID}:{i + 1}", "passing", default(CancellationToken));


                                            LogDebug("service " + registration.ID + " ttl passing", Array.Empty<object>());
                                        }
                                        else if(status== CheckStatus.Warning)
                                        {
                                            await _client.Agent.WarnTTL($"service:{registration.ID}:{i + 1}", "passing", default(CancellationToken));


                                            LogDebug("service " + registration.ID + " ttl warn", Array.Empty<object>());

                                        }
                                        else 
                                        {
                                            await _client.Agent.FailTTL($"service:{registration.ID}:{i + 1}", "passing", default(CancellationToken));


                                            LogDebug("service " + registration.ID + " ttl warn", Array.Empty<object>());

                                        }
                                    }
                                    catch (Exception ex)
                                    {

                                        LogWarning(ex, ex.Message);
                                    }
                                }
                            }
                        }
                        else if (registration.Checks.Length == 0)
                        {
                            if (registration.Checks[0].TTL.HasValue)
                            {
                                try
                                {
                                    if (status == CheckStatus.Healthy)
                                    {

                                        await _client.Agent.PassTTL($"service:{registration.ID}", "passing", default(CancellationToken));


                                        LogDebug("service " + registration.ID + " ttl passing", Array.Empty<object>());
                                    }
                                    else if (status == CheckStatus.Warning)
                                    {
                                        await _client.Agent.WarnTTL($"service:{registration.ID}", "passing", default(CancellationToken));


                                        LogDebug("service " + registration.ID + " ttl warn", Array.Empty<object>());

                                    }
                                    else
                                    {
                                        await _client.Agent.FailTTL($"service:{registration.ID}", "passing", default(CancellationToken));


                                        LogDebug("service " + registration.ID + " ttl warn", Array.Empty<object>());

                                    }
                                }
                                catch (Exception ex)
                                {

                                    LogWarning(ex, ex.Message);
                                }
                            }
                        }
                    }

                }
                catch (Exception ex)
                {
                    LogError(ex, ex.Message);
                }
            }
        }

        public async void Register()
        {
            var policy = Policy.Handle<Exception>().WaitAndRetryForever((int a) => TimeSpan.FromSeconds(5.0), delegate (Exception ex, TimeSpan time)
            {
                _logger.LogError(ex, ex.Message, Array.Empty<object>());
            });

            foreach (AgentServiceRegistration item3 in _registrations)
            {
                await policy.Execute(async ()=>
                {
                    LogInformation("service " + item3.ID + " registration", Array.Empty<object>());
                    WriteResult ret3 = await _client.Agent.ServiceRegister(item3, default(CancellationToken));
                    if(ret3.StatusCode!= HttpStatusCode.OK)
                    {
                        throw new Exception("service {item3.ID} register failed");
                    }
                    else
                    {
                        registerCompleted = true;
                    }

                    LogInformation($"service {item3.ID} registered. time={ret3.RequestTime},statusCode={ret3.StatusCode}", Array.Empty<object>());
                });
            }
        }

        public async void Deregister()
        {
            var policy = Policy.Handle<Exception>().Or<IOException>().WaitAndRetryForever((int a) => TimeSpan.FromSeconds(5.0), delegate (Exception ex, TimeSpan time)
            {
                _logger.LogError(ex, ex.Message, Array.Empty<object>());
            });

            foreach (AgentServiceRegistration item4 in _registrations)
            {
                await policy.Execute(async delegate
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
