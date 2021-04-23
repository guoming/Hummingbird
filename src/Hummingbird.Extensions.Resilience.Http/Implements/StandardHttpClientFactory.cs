using Hummingbird.DynamicRoute;
using Hummingbird.Extensions.Resilience.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Polly;
using System;
using System.Net.Http;

namespace Hummingbird.Extensions.Resilience.Http
{
    public class StandardHttpClientFactory : IHttpClientFactory
    {
        private readonly ILogger<StandardHttpClient> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IServiceLocator _serviceLocator;

        public StandardHttpClientFactory(
            ILogger<StandardHttpClient> logger, 
            IHttpContextAccessor httpContextAccessor,
            IServiceLocator serviceLocator)
        {
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
            _serviceLocator = serviceLocator;
        }

        public IHttpClient CreateResilientHttpClient()
            => new StandardHttpClient(_logger, _httpContextAccessor, new HttpUrlResolver(_serviceLocator));
        public IHttpClient CreateResilientHttpClient(HttpMessageHandler httpMessageHandler)
        => new StandardHttpClient(_logger, _httpContextAccessor, new HttpUrlResolver(_serviceLocator), httpMessageHandler);


        
    }
}
