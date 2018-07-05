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

            _cache.Add("11", "test", TimeSpan.FromMinutes(5), "ProjectName:Enviroment1:Regsion1");
            _cache.Add("22", "test", TimeSpan.FromMinutes(5), "ProjectName:Enviroment2:Regsion1");
            _cache.Add("33", "test", TimeSpan.FromMinutes(5), "ProjectName:Enviroment1:Regsion2");
            _cache.Add("44", "test", TimeSpan.FromMinutes(5), "ProjectName:Enviroment2:Regsion2");
        }

        [HttpGet]
        [Route("Healthcheck")]
        public async Task<string> Healthcheck()
        {
            return await Task.FromResult("Ok");
        }

        [HttpGet]
        public async Task<string> Get()
        {

            await _eventBus.PublishAsync(new System.Collections.Generic.List<Hummingbird.Extersions.EventBus.Models.EventLogEntry>()
            {
                new Hummingbird.Extersions.EventBus.Models.EventLogEntry(new User{

                     Name=Guid.NewGuid().ToString("N")
                }),
                new Hummingbird.Extersions.EventBus.Models.EventLogEntry(new Hummingbird.WebApi.Events.NewMsgEvent(){
                    Time=DateTime.Now
                })

            });

            var result = await _httpClient.GetStringAsync("http://route.showapi.com/64-19?com=zhongtong&nul=535962308717");
            return result;

        }

        [HttpPost]
        public async void Post([FromBody]string value)
        {
            var result = await _httpClient.PostAsync("http://route.showapi.com/64-19", value);
        }

        [HttpPut("{id}")]
        public async void Put(int id, [FromBody]string value)
        {
            var result = await _httpClient.PutAsync("http://route.showapi.com/64-19", value);

        }

        // DELETE api/values/5
        [HttpDelete("{id}")]
        public async void Delete(int id)
        {
            var result = await _httpClient.DeleteAsync("http://route.showapi.com/64-19");
        }
    }
}
