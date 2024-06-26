
using System;
using System.Text.Json.Serialization;

namespace Hummingbird.Example.Controllers
{
    using Hummingbird.Extensions.Cacheing;
    using Microsoft.AspNetCore.Mvc;
    using System.Threading.Tasks;

    [Route("api/[controller]")]
    public class CacheingController : BaseController
    {
        private readonly ICacheManager _cacheManager;

        public CacheingController(
            ICacheManager cacheManager)
        {
            this._cacheManager = cacheManager;
        }

   

        [HttpGet]
        [Route("Test/{cacheKey}")]
        public  string Test(string cacheKey="key1")
        {
            var cacheValue = _cacheManager.StringGet<string>(cacheKey);
            if(cacheValue == null)
            {
                cacheValue = "value";
                _cacheManager.StringSet(cacheKey, cacheValue);
            }

            return cacheValue;

        }
        
        
        [HttpGet]
        [Route("Test2/{cacheKey}")]
        public  string Test2(string cacheKey="key1")
        {
            var cacheValue = _cacheManager.StringGet<object>(cacheKey);
            if(cacheValue == null)
            {
                cacheValue = new
                {
                    name = Guid.NewGuid()
                };
                _cacheManager.StringSet(cacheKey, cacheValue);
            }

            return Newtonsoft.Json.JsonConvert.SerializeObject(cacheValue);

        }

    }

}
