using Hummingbird.Resilience.Http;
using System;

namespace Hummingbird.Resilience.Http
{
    public interface IHttpClientFactory
    {
        IHttpClient CreateResilientHttpClient();
    }
}