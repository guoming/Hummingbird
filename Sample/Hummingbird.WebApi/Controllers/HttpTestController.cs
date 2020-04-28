using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Hummingbird.DynamicRoute;
using Hummingbird.Extensions.Cache;
using Hummingbird.Extensions.Cacheing;
using Hummingbird.Extensions.EventBus.Abstractions;
using Hummingbird.Extensions.Resilience.Http;
using Hummingbird.LoadBalancers;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
namespace Hummingbird.Example.Controllers
{
  
    [Route("api/[controller]")]
    public class HttpClientTestController : Controller
    {
        private readonly IHttpClient httpClient;
        public HttpClientTestController(IHttpClient httpClient)
        {
            this.httpClient = httpClient;
        }
      

        [HttpGet]
        [Route("Publish2")]
        public async Task<string> Publish2()
        {
           return await (await  httpClient.PostAsync("http://baidu.com", new { name = "123" }, null, null)).Content.ReadAsStringAsync();
        }

    }

}
