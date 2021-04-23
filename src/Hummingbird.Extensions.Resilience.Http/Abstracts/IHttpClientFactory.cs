using Hummingbird.Extensions.Resilience.Http;
using System;
using System.Net.Http;

namespace Hummingbird.Extensions.Resilience.Http
{
    public interface IHttpClientFactory
    {
        IHttpClient CreateResilientHttpClient();

        IHttpClient CreateResilientHttpClient(HttpMessageHandler httpMessageHandler);
    }
}