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
            Hummingbird.Extensions.Resilience.Http.IHttpClient httpClient,
            Hummingbird.Extensions.EventBus.Abstractions.IEventBus eventBus)
        {
            this.httpClient = httpClient;
            this.eventBus = eventBus;
        }

        private readonly IHttpClient httpClient;
        private readonly IEventBus eventBus;

        [HttpGet]
        [Route("Publish")]
        public async Task<string> Publish()
        {
          var ret=  await  eventBus.PublishAsync(new List<Hummingbird.Extensions.EventBus.Models.EventLogEntry>() {
                new Hummingbird.Extensions.EventBus.Models.EventLogEntry("TestTopic",new { Name="郭明",Age=1 }),
                   new Hummingbird.Extensions.EventBus.Models.EventLogEntry("TestTopic",new { Name="郭明2",Age=2 }),
            });

            return ret.ToString();
        }

        [HttpGet]
        [Route("Publish2")]
        public async Task<string> Publish2()
        {
           return await (await  httpClient.PostAsync("http://baidu.com", new { name = "123" }, null, null)).Content.ReadAsStringAsync();
        }

    }

}
