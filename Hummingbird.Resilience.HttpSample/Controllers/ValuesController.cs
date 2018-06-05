using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Hummingbird.Resilience.Http;
using Microsoft.AspNetCore.Mvc;
namespace DotNetCore.Resilience.HttpSample.Controllers
{
    [Route("api/[controller]")]
    public class ValuesController : Controller
    {
        IHttpClient _httpClient;
        Hummingbird.Cache.IHummingbirdCache<string> _cache;

        public ValuesController(IHttpClient httpClient,Hummingbird.Cache.IHummingbirdCache<string> cache)
        {
            _httpClient = httpClient;
            _cache = cache;

            _cache.Add("11", "test", TimeSpan.FromMinutes(5), "ProjectName:Enviroment1:Regsion1");
            _cache.Add("22", "test", TimeSpan.FromMinutes(5), "ProjectName:Enviroment2:Regsion1");
            _cache.Add("33", "test", TimeSpan.FromMinutes(5), "ProjectName:Enviroment1:Regsion2");
            _cache.Add("44", "test", TimeSpan.FromMinutes(5), "ProjectName:Enviroment2:Regsion2");
        }

        [HttpGet]
        public async Task<string> Get()
        {
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
