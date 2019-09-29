using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Polly;
using Polly.Wrap;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Hummingbird.Extersions.Resilience.Http
{
    /// <summary>
    /// 弹性HTTP连接客户端，集成重试和熔断策略
    /// 作者：郭明
    /// 日期：2017年11月17日
    /// </summary>
    public class ResilientHttpClient : IHttpClient
    {
        private readonly HttpClient _client;
        private readonly ILogger<ResilientHttpClient> _logger;
        private readonly Func<string, IEnumerable<Policy>> _policyCreator;
        private ConcurrentDictionary<string, PolicyWrap> _policyWrappers;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ResilientHttpClient(
            Func<string, IEnumerable<Policy>> policyCreator,
            ILogger<ResilientHttpClient> logger,
            IHttpContextAccessor httpContextAccessor)
        {
            _client = new HttpClient();
            _logger = logger;
            _policyCreator = policyCreator;
            _policyWrappers = new ConcurrentDictionary<string, PolicyWrap>();
            _httpContextAccessor = httpContextAccessor;
        }


        public Task<HttpResponseMessage> PostAsync<T>(string uri, T item, string authorizationToken = null, string authorizationMethod = "Bearer", IDictionary<string, string> dictionary = null)
        {
            return DoPostPutAsync(HttpMethod.Post, uri, item, authorizationToken,  authorizationMethod, dictionary);
        }

        public Task<HttpResponseMessage> PutAsync<T>(string uri, T item, string authorizationToken = null, string authorizationMethod = "Bearer", IDictionary<string, string> dictionary = null)
        {
            return DoPostPutAsync(HttpMethod.Put, uri, item, authorizationToken,  authorizationMethod, dictionary);
        }

        public Task<HttpResponseMessage> DeleteAsync(string uri, string authorizationToken = null, string authorizationMethod = "Bearer", IDictionary<string, string> dictionary = null)
        {
            var origin = GetOriginFromUri(uri);

            return HttpInvoker(origin, async (context) =>
            {
                var requestMessage = new HttpRequestMessage(HttpMethod.Delete, uri);

                SetAuthorizationHeader(requestMessage);

                if (authorizationToken != null)
                {
                    requestMessage.Headers.Authorization = new AuthenticationHeaderValue(authorizationMethod, authorizationToken);
                }

                if (dictionary != null)
                {
                    foreach (var key in dictionary.Keys)
                    {
                        requestMessage.Headers.Add(key, dictionary[key]);
                    }
                }

                return await _client.SendAsync(requestMessage);
            });
        }


        public Task<string> GetStringAsync(string uri, string authorizationToken = null, string authorizationMethod = "Bearer", IDictionary<string, string> dictionary = null)
        {
            var origin = GetOriginFromUri(uri);

            return HttpInvoker(origin, async (context) =>
            {
                var requestMessage = new HttpRequestMessage(HttpMethod.Get, uri);

                SetAuthorizationHeader(requestMessage);

                if (authorizationToken != null)
                {
                    requestMessage.Headers.Authorization = new AuthenticationHeaderValue(authorizationMethod, authorizationToken);
                }

                if (dictionary != null)
                {
                    foreach (var key in dictionary.Keys)
                    {
                        requestMessage.Headers.Add(key, dictionary[key]);
                    }
                }

                var response = await _client.SendAsync(requestMessage);

                if (response.StatusCode == HttpStatusCode.InternalServerError)
                {
                    throw new HttpRequestException();
                }

                return await response.Content.ReadAsStringAsync();
            });
        }

        private Task<HttpResponseMessage> DoPostPutAsync<T>(HttpMethod method, string uri, T item, string authorizationToken = null, string authorizationMethod = "Bearer", IDictionary<string, string> dictionary = null)
        {
            if (method != HttpMethod.Post && method != HttpMethod.Put)
            {
                throw new ArgumentException("Value must be either post or put.", nameof(method));
            }

            var origin = GetOriginFromUri(uri);

            return HttpInvoker(origin, async (context) =>
           {
               var requestMessage = new HttpRequestMessage(method, uri);

               SetAuthorizationHeader(requestMessage);

               requestMessage.Content = new StringContent(JsonConvert.SerializeObject(item), System.Text.Encoding.UTF8, "application/json");

               if (authorizationToken != null)
               {
                   requestMessage.Headers.Authorization = new AuthenticationHeaderValue(authorizationMethod, authorizationToken);
               }

               if (dictionary != null)
               {
                   foreach (var key in dictionary.Keys)
                   {
                       requestMessage.Headers.Add(key, dictionary[key]);
                   }
               }

               var response = await _client.SendAsync(requestMessage);

               if (response.StatusCode == HttpStatusCode.InternalServerError)
               {
                   throw new HttpRequestException();
               }

               return response;
           });
        }

        private async Task<T> HttpInvoker<T>(string origin, Func<Context, Task<T>> action)
        {
            var normalizedOrigin = NormalizeOrigin(origin);

            if (!_policyWrappers.TryGetValue(normalizedOrigin, out PolicyWrap policyWrap))
            {
                policyWrap = Policy.WrapAsync(_policyCreator(normalizedOrigin).ToArray());
                _policyWrappers.TryAdd(normalizedOrigin, policyWrap);
            }

            return await policyWrap.ExecuteAsync(action, new Context(normalizedOrigin));
        }


        private static string NormalizeOrigin(string origin)
        {
            return origin?.Trim()?.ToLower();
        }

        private static string GetOriginFromUri(string uri)
        {
            var url = new Uri(uri);

            var origin = $"{url.Scheme}://{url.DnsSafeHost}:{url.Port}";

            return origin;
        }

        private void SetAuthorizationHeader(HttpRequestMessage requestMessage)
        {
            if (_httpContextAccessor.HttpContext != null)
            {
                var authorizationHeader = _httpContextAccessor.HttpContext.Request.Headers["Authorization"];
                if (!string.IsNullOrEmpty(authorizationHeader))
                {
                    requestMessage.Headers.Add("Authorization", new List<string>() { authorizationHeader });
                }
            }
        }
    }
}
