using System;
using System.Collections.Generic;
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
    public class TestController : Controller
    {
        private readonly IEventBus _eventBus;
        private readonly IEventLogger _eventLogger;

        public TestController(
            IEventBus eventBus,
            IEventLogger  eventLogger)
        {
            _eventBus = eventBus;
            _eventLogger = eventLogger;
        }

        [HttpGet]
        [Route("Empty")]
        public async Task Empty()
        {
           
        }


        [HttpGet]
        [Route("PublishNonConfirm")]
        public async Task PublishNonConfirmAsync()
        {
            for (int i = 0; i < 100000; i++)
            {
                await _eventBus.PublishNonConfirmAsync(new System.Collections.Generic.List<Hummingbird.Extersions.EventBus.Models.EventLogEntry>(){

                        new Hummingbird.Extersions.EventBus.Models.EventLogEntry("NewMsgEvent",new Hummingbird.WebApi.Events.NewMsgEvent{

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

                        new Hummingbird.Extersions.EventBus.Models.EventLogEntry("NewMsgEvent",new Hummingbird.WebApi.Events.NewMsgEvent{

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
