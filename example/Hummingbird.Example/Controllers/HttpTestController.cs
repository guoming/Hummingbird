
namespace Hummingbird.Example.Controllers
{
    using Hummingbird.Extensions.Resilience.Http;
    using Microsoft.AspNetCore.Mvc;
    using System.Threading;
    using System.Threading.Tasks;
    [Route("api/[controller]")]
    public class HttpClientTestController : Controller
    {
        private readonly IHttpClient httpClient;
        public HttpClientTestController(IHttpClient httpClient)
        {
            this.httpClient = httpClient;
        }
      

        [HttpGet]
        [Route("Test1")]
        public async Task<string> Test1()
        {
            return await httpClient.GetStringAsync("http://baidu.com");
        }

        [HttpGet]
        [Route("Test2")]
        public async Task<string> Test2()
        {
            return await httpClient.GetStringAsync(
                uri: "http://{example}/healthcheck",
                authorizationMethod: null,
                authorizationToken: null,
                dictionary: null);
        }

    }

}
