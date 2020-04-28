
namespace Hummingbird.Example.Controllers
{
    using Microsoft.AspNetCore.Mvc;
    using System.Threading.Tasks;
    [Route("api/[controller]")]
    public class OpenTracingTestController : Controller
    {
       

        [HttpGet]
        [Route("Test")]
        public async Task Test()
        {
            using (Hummingbird.Extensions.Tracing.Tracer tracer = new Hummingbird.Extensions.Tracing.Tracer("Test"))
            {
                tracer.SetTag("tag1", "value1");
                tracer.SetError();
                tracer.Log("key1", "value1");

            }
        }

    }

}
