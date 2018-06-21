using Hummingbird.Extersions.Resilience.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Polly;
using System;
using System.Net.Http;

namespace Hummingbird.Extersions.Resilience.Http
{
    public class ResilientHttpClientFactory : IHttpClientFactory
    {
        private readonly ILogger<ResilientHttpClient> _logger;
        private readonly int _retryCount;
        private readonly int _exceptionsAllowedBeforeBreaking;
        private readonly int _durationSecondsOfBreak;
        private readonly int _timeoutMillseconds;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ResilientHttpClientFactory(
            ILogger<ResilientHttpClient> logger, 
            IHttpContextAccessor httpContextAccessor, 
            int timeoutMillseconds=200,
            int exceptionsAllowedBeforeBreaking = 5, 
            int retryCount = 6,
            int durationSecondsOfBreak = 60)
        {

            _logger = logger;
            _timeoutMillseconds = timeoutMillseconds;
            _exceptionsAllowedBeforeBreaking = exceptionsAllowedBeforeBreaking;
            _retryCount = retryCount;
            _httpContextAccessor = httpContextAccessor;
            _durationSecondsOfBreak = durationSecondsOfBreak;
        }


        public IHttpClient CreateResilientHttpClient()
            => new ResilientHttpClient((origin) => CreatePolicies(), _logger, _httpContextAccessor);

        private Policy[] CreatePolicies()
            => new Policy[]
            {
                Policy.Handle<HttpRequestException>()
                .WaitAndRetryAsync(
                    // 重试次数
                    _retryCount,
                    // 指数退避算法
                    retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
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
                   _exceptionsAllowedBeforeBreaking,
                   //熔断后，需要等待多久不想回复
                   TimeSpan.FromSeconds(_durationSecondsOfBreak),
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
    }
}
