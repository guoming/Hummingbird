using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Hummingbird.Extersions.Cache;
using Hummingbird.Extersions.Cacheing;
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
    public class TestController : Controller
    {
        private readonly ICacheManager cacheManager;
        private readonly IEventBus _eventBus;
        private readonly IEventLogger _eventLogger;

        public TestController(
            ICacheManager cacheManager,
            IEventBus eventBus,
            IEventLogger  eventLogger)
        {
            this.cacheManager = cacheManager;
            _eventBus = eventBus;
            _eventLogger = eventLogger;
        }

        volatile int count = 0;

        [HttpGet]
        [Route("Empty")]
        public async Task Empty()
        {

            Parallel.For(0, 10000000,new ParallelOptions() {  MaxDegreeOfParallelism=50}, (i) =>
              {


                  var ret2 = (int)cacheManager.Execute("BF.ADD", "TrackingNumbers", i);
                  System.Threading.Interlocked.Add(ref count, 1);

                  Console.WriteLine($"ADD:{i}:{count}");

              });

            Parallel.For(0, 10000000, new ParallelOptions() { MaxDegreeOfParallelism = 20 }, (i) =>
            {
                var ret2 = (int)cacheManager.Execute("BF.EXISTS", "TrackingNumbers", i) == 1;
                System.Threading.Interlocked.Add(ref count, 1);

                Console.WriteLine($"EXISTS:{i}:{count}");
             });


        }


        [HttpGet]
        [Route("PublishNonConfirm")]
        public async Task PublishNonConfirmAsync()
        {
            for (int i = 0; i < 100000; i++)
            {
                await _eventBus.PublishNonConfirmAsync(new System.Collections.Generic.List<Hummingbird.Extersions.EventBus.Models.EventLogEntry>(){

                        new Hummingbird.Extersions.EventBus.Models.EventLogEntry("NewMsgEvent",new  {

                                Value=i
                            })
                });
            }
        }


        [HttpGet]
        [Route("Publish")]
        public async Task<bool> PublishAsync()
        {
            var result = new List<string>();

            for (int i = 0; i < 100000; i++)
            {

                var r= await _eventBus.PublishAsync(new System.Collections.Generic.List<Hummingbird.Extersions.EventBus.Models.EventLogEntry>(){

                        new Hummingbird.Extersions.EventBus.Models.EventLogEntry("NewMsgEvent",new {

                                Value=0
                            })
                });

                if(!r)
                {
                    Console.WriteLine("F");
                }
                else
                {
                    Console.WriteLine("O");
                }
            }




            return true;
        }
    }
}
