using Hummingbird.Extersions.Resilience.Http;
using System;

namespace Hummingbird.Extersions.Resilience.Http
{
    public interface IHttpClientFactory
    {
        IHttpClient CreateResilientHttpClient();
    }
}