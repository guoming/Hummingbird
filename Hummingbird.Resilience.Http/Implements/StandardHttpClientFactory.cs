using Hummingbird.Resilience.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Polly;
using System;
using System.Net.Http;

namespace Hummingbird.Resilience.Http
{
    public class StandardHttpClientFactory : IHttpClientFactory
    {
        private readonly ILogger<StandardHttpClient> _logger;
        private readonly int _retryCount;
        private readonly int _exceptionsAllowedBeforeBreaking;
        private readonly int _durationSecondsOfBreak;
        private readonly int _timeoutMillseconds;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public StandardHttpClientFactory(
            ILogger<StandardHttpClient> logger, 
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
            => new StandardHttpClient(_logger, _httpContextAccessor);
 
    }
}
