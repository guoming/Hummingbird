
namespace Hummingbird.Example.Controllers
{
    using Hummingbird.Extensions.Cacheing;
    using Microsoft.AspNetCore.Mvc;
    using System.Threading.Tasks;

    [Route("api/[controller]")]
    public class CacheingController : Controller
    {
        private readonly ICacheManager cacheManager;

        public CacheingController(
            ICacheManager cacheManager)
        {
            this.cacheManager = cacheManager;
        }

   

        [HttpGet]
        [Route("Test/{cacheKey}")]
        public  string Test(string cacheKey="key1")
        {
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
