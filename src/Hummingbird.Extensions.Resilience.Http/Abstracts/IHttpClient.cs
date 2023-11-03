using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Threading;

namespace Hummingbird.Extensions.Resilience.Http
{
    public interface IHttpClient
    {
        Task<HttpResponseMessage> UploadAsync(
            string uri,
            List<FileStream> files,
            IDictionary<string, string> item,
            string authorizationToken = null,
            string authorizationMethod = "Bearer",
            IDictionary<string, string> dictionary = null,
            CancellationToken cancellationToken = default(CancellationToken));
        
        Task<string> GetStringAsync(string uri, string authorizationToken = null, string authorizationMethod = "Bearer",IDictionary<string,string> dictionary=null, CancellationToken cancellationToken=default(CancellationToken));

        Task<HttpResponseMessage> PostAsync<T>(string uri, T item, string authorizationToken = null,string authorizationMethod = "Bearer", IDictionary<string, string> dictionary = null, CancellationToken cancellationToken = default(CancellationToken));

        Task<HttpResponseMessage> DeleteAsync(string uri, string authorizationToken = null,  string authorizationMethod = "Bearer", IDictionary<string, string> dictionary = null, CancellationToken cancellationToken = default(CancellationToken));

        Task<HttpResponseMessage> PutAsync<T>(string uri, T item, string authorizationToken = null,  string authorizationMethod = "Bearer", IDictionary<string, string> dictionary = null, CancellationToken cancellationToken = default(CancellationToken));
    }
}

