using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace Hummingbird.Extersions.Resilience.Http
{
    public interface IHttpClient
    {
        Task<string> GetStringAsync(string uri, string authorizationToken = null, string authorizationMethod = "Bearer",IDictionary<string,string> dictionary=null);

        Task<HttpResponseMessage> PostAsync<T>(string uri, T item, string authorizationToken = null,string authorizationMethod = "Bearer", IDictionary<string, string> dictionary = null);

        Task<HttpResponseMessage> DeleteAsync(string uri, string authorizationToken = null,  string authorizationMethod = "Bearer", IDictionary<string, string> dictionary = null);

        Task<HttpResponseMessage> PutAsync<T>(string uri, T item, string authorizationToken = null,  string authorizationMethod = "Bearer", IDictionary<string, string> dictionary = null);
    }
}
