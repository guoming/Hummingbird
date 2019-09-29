using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Hummingbird.Extersions.Resilience.Http
{
    public static class HttpResponseMessageExtensions
    {
        public static async Task<TResponse> ReadAsObjectAsync<TResponse>(this HttpResponseMessage response)
        {
            var json = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<TResponse>(json);
        }
    }
}
