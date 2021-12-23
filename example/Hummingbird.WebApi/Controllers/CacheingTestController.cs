
namespace Hummingbird.Example.Controllers
{
    using Hummingbird.Extensions.Cacheing;
    using Microsoft.AspNetCore.Mvc;
    using System.Threading.Tasks;

    [Route("api/[controller]")]
    public class CacheingTestController : Controller
    {
        private readonly ICacheManager cacheManager;

        public CacheingTestController(
            ICacheManager cacheManager)
        {
            this.cacheManager = cacheManager;
        }

   

        [HttpGet]
        [Route("Test")]
        public  string Test()
        {
            var cacheKey = "cacheKey";
            var cacheValue = cacheManager.StringGet<string>(cacheKey);
            if(cacheValue == null)
            {
                cacheValue = "value";
                cacheManager.StringSet(cacheKey, cacheValue);
            }

            return cacheValue;

        }

    }

}
