using System;
using System.Net.Http;
using System.Threading.Tasks;
using Hummingbird.Extersions.Cache;
using Hummingbird.Extersions.EventBus.Abstractions;
using Hummingbird.Extersions.Resilience.Http;

using Microsoft.AspNetCore.Mvc;
namespace DotNetCore.Resilience.HttpSample.Controllers
{
    public class User
    {
        public string Name { get; set; }
    }

    [Route("api/[controller]")]
    public class ValuesController : Controller
    {
        IHttpClient _httpClient;
        IHummingbirdCache<string> _cache;
        private readonly IEventBus _eventBus;



        public ValuesController(IEventBus eventBus, IHttpClient httpClient, IHummingbirdCache<string> cache)
        {
            _eventBus = eventBus;
            _httpClient = httpClient;
            _cache = cache;
        }


        [HttpGet]
        public async Task<string> Get()
        {
            System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
            stopwatch.Start();
            for (var i = 0; i < 1000; i++)
            {
                await _eventBus.PublishAsync(new System.Collections.Generic.List<Hummingbird.Extersions.EventBus.Models.EventLogEntry>(){

                        new Hummingbird.Extersions.EventBus.Models.EventLogEntry(new User{

                                 Name=Guid.NewGuid().ToString("N")
                            }),
                            new Hummingbird.Extersions.EventBus.Models.EventLogEntry(new Hummingbird.WebApi.Events.NewMsgEvent(){
                                Time=DateTime.Now
                            })
                });
            }

            stopwatch.Stop();

            return $"花费{stopwatch.ElapsedMilliseconds}毫秒";

        }
    }
}
