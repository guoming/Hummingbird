using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Hummingbird.DynamicRoute;
using Hummingbird.Extersions.Cache;
using Hummingbird.Extersions.Cacheing;
using Hummingbird.Extersions.EventBus.Abstractions;
using Hummingbird.Extersions.Resilience.Http;
using Hummingbird.LoadBalancers;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
namespace DotNetCore.Resilience.HttpSample.Controllers
{
    public class User
    {
        public string Name { get; set; }
    }

    [Route("api/[controller]")]
    public class TestController : Controller
    {

        public TestController(
            IHttpClient httpClient)
        {
            this.httpClient = httpClient;
        }

        volatile int count = 0;
        private readonly IHttpClient httpClient;

        [HttpGet]
        [Route("Empty")]
        public async Task<string> Case1()
        {
            return await httpClient.GetStringAsync("http://localhost:40740/api/Test/Sleep10");
        }

        [HttpGet]
        [Route("Sleep10")]
        public async Task<string> Sleep10()
        {
            System.Threading.Thread.Sleep(10000 * 20);
            return "ok";
        }
    }

}
