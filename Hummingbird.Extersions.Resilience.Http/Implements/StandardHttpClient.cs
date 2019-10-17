using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace Hummingbird.Extersions.Resilience.Http
{
    public class StandardHttpClient : IHttpClient
    {
        private HttpClient _client;
        private ILogger<StandardHttpClient> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public StandardHttpClient(ILogger<StandardHttpClient> logger, IHttpContextAccessor httpContextAccessor)
        {
            _client = new HttpClient();
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<string> GetStringAsync(string uri, string authorizationToken = null, string authorizationMethod = "Bearer", IDictionary<string, string> dictionary = null)
        {
            using (var tracer = new Hummingbird.Extensions.Tracing.Tracer("HTTP GET"))
            {
                tracer.SetComponent("ResilientHttpClient");
                tracer.SetTag("http.url", uri);
                tracer.SetTag("http.method", "GET");
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

                var responseMessage = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("response", responseMessage);

                return responseMessage;
            }
        }

        public async Task<HttpResponseMessage> PostAsync<T>(string uri, T item, string authorizationToken = null,  string authorizationMethod = "Bearer", IDictionary<string, string> dictionary = null)
        {
            using (var tracer = new Hummingbird.Extensions.Tracing.Tracer("HTTP POST"))
            {
                tracer.SetComponent("StandardHttpClient");
                tracer.SetTag("http.url", uri);
                tracer.SetTag("http.method", "POST");
                return await DoPostPutAsync(HttpMethod.Post, uri, item, authorizationToken, authorizationMethod, dictionary);
            }
        }

        public async Task<HttpResponseMessage> PutAsync<T>(string uri, T item, string authorizationToken = null, string authorizationMethod = "Bearer", IDictionary<string, string> dictionary = null)
        {
            using (var tracer = new Hummingbird.Extensions.Tracing.Tracer("HTTP PUT"))
            {
                tracer.SetComponent("StandardHttpClient");
                tracer.SetTag("http.url", uri);
                tracer.SetTag("http.method", "PUT");
                return await DoPostPutAsync(HttpMethod.Put, uri, item, authorizationToken, authorizationMethod, dictionary);
            }
        }

        public async Task<HttpResponseMessage> DeleteAsync(string uri, string authorizationToken = null, string authorizationMethod = "Bearer", IDictionary<string, string> dictionary = null)
        {
            using (var tracer = new Hummingbird.Extensions.Tracing.Tracer("HTTP DELETE"))
            {
                tracer.SetComponent("StandardHttpClient");
                tracer.SetTag("http.url", uri);
                tracer.SetTag("http.method", "DELETE");

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

                var response = await _client.SendAsync(requestMessage);

                var responseMessage = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("response", responseMessage);

                return response;
            }
        }

        private void SetAuthorizationHeader(HttpRequestMessage requestMessage)
        {
            var authorizationHeader = _httpContextAccessor.HttpContext.Request.Headers["Authorization"];
            if (!string.IsNullOrEmpty(authorizationHeader))
            {
                requestMessage.Headers.Add("Authorization", new List<string>() { authorizationHeader });
            }
        }

        private async Task<HttpResponseMessage> DoPostPutAsync<T>(HttpMethod method, string uri, T item, string authorizationToken = null, string authorizationMethod = "Bearer", IDictionary<string, string> dictionary = null)
        {
            if (method != HttpMethod.Post && method != HttpMethod.Put)
            {
                throw new ArgumentException("Value must be either post or put.", nameof(method));
            }

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

            _logger.LogInformation("request", item);

            var response = await _client.SendAsync(requestMessage);

            if (response.StatusCode == HttpStatusCode.InternalServerError)
            {
                throw new HttpRequestException();
            }

            var responseMessage = await response.Content.ReadAsStringAsync();
            _logger.LogInformation("response", responseMessage);

            return response;
        }

    }
}

