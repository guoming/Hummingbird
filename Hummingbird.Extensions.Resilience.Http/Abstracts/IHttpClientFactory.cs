using Hummingbird.Extensions.Resilience.Http;
using System;

namespace Hummingbird.Extensions.Resilience.Http
{
    public interface IHttpClientFactory
    {
        IHttpClient CreateResilientHttpClient();
    }
}