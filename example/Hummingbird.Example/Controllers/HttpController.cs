
namespace Hummingbird.Example.Controllers
{
    using Hummingbird.Extensions.Resilience.Http;
    using Microsoft.AspNetCore.Mvc;
    using System.Threading;
    using System.Threading.Tasks;
    [Route("api/[controller]")]
    public class HttpController : Controller
    {
        private readonly IHttpClient httpClient;
        public HttpController(IHttpClient httpClient)
        {
            this.httpClient = httpClient;
        }
      

        [HttpGet]
        [Route("TestStaticRoute")]
        public async Task<string> TestStaticRoute()
        {
            return await httpClient.GetStringAsync("http://localhost:8080/healthcheck");
        }

        [HttpGet]
        [Route("TestDynamicRoute")]
        public async Task<string> TestDynamicRoute()
        {
            return await httpClient.GetStringAsync(
                uri: "http://{example}/healthcheck",
                authorizationMethod: null,
                authorizationToken: null,
                dictionary: null);
        }

    }

}
