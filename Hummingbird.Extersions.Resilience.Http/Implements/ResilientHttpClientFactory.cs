using Hummingbird.Extersions.Resilience.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Polly;
using System;
using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;
using Hummingbird.DynamicRoute;

namespace Hummingbird.Extersions.Resilience.Http
{
    public class ResilientHttpClientFactory : IHttpClientFactory
    {
        private readonly IServiceLocator _serviceLocator;
        private readonly Action<string, ResilientHttpClientConfigOption> _func;
        private readonly ILogger<ResilientHttpClient> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ResilientHttpClientFactory(
            ILogger<ResilientHttpClient> logger,
            IHttpContextAccessor httpContextAccessor,
            IServiceLocator serviceLocator,
            Action<string, ResilientHttpClientConfigOption> func)
        {
            _serviceLocator = serviceLocator;
            _func = func;
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;

        }


        public IHttpClient CreateResilientHttpClient()
            => new ResilientHttpClient((origin) => CreatePolicies(origin), _logger, _httpContextAccessor, new HttpUrlResolver(_serviceLocator));

        private IAsyncPolicy[] CreatePolicies(string origin)
        {
            var option = new ResilientHttpClientConfigOption()
            {
                TimeoutMillseconds = 1000 * 120,
                RetryCount = 3,
                DurationSecondsOfBreak = 15,
                ExceptionsAllowedBeforeBreaking = 10
            };

            _func(origin, option);

            var result = new IAsyncPolicy[]
            {
                    Policy.TimeoutAsync(TimeSpan.FromMilliseconds(option.TimeoutMillseconds), Polly.Timeout.TimeoutStrategy.Pessimistic),
                    Policy.Handle<HttpRequestException>()
                    .WaitAndRetryAsync(
                        // 重试次数
                        option.RetryCount,
                        // 指数退避算法
                        retryAttempt => TimeSpan.FromMilliseconds(Math.Pow(2, retryAttempt)),
                        // 重试是执行的方法
                        (exception, timeSpan, retryCount, context) =>
                        {
                            var msg = $"Retry {retryCount} implemented with Polly's RetryPolicy " +
                                $"of {context.PolicyKey} " +
                                $"at {context.OperationKey}, " +
                                $"due to: {exception}.";
                            _logger.LogWarning(msg);
                            _logger.LogDebug(msg);

                        }),
                    Policy.Handle<HttpRequestException>()
                    .CircuitBreakerAsync(
                       // 异常阀值，超过时熔断
                       option.ExceptionsAllowedBeforeBreaking,
                       //熔断后，需要等待多久不想回复
                       TimeSpan.FromSeconds(option.DurationSecondsOfBreak),
                       (exception, duration) =>
                       {
                           //打开已经打开
                           _logger.LogTrace("Circuit breaker opened");
                       },
                       () =>
                       {
                           //熔断已经关闭
                           _logger.LogTrace("Circuit breaker reset");
                       })
           };
            return result;


        }
    }
}
