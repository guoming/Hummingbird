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
            await _eventBus.PublishNonConfirmAsync(new System.Collections.Generic.List<Hummingbird.Extersions.EventBus.Models.EventLogEntry>(){

                        new Hummingbird.Extersions.EventBus.Models.EventLogEntry("NewMsgEvent",new Hummingbird.WebApi.Events.NewMsgEvent{

                                Time=DateTime.Now
                            }),
                            new Hummingbird.Extersions.EventBus.Models.EventLogEntry("NewMsgEvent",new Hummingbird.WebApi.Events.NewMsgEvent(){
                                Time=DateTime.Now
                            })
                });
        }


        [HttpGet]
        [Route("Publish")]
        public async Task PublishAsync()
        {
            await _eventBus.PublishAsync(new System.Collections.Generic.List<Hummingbird.Extersions.EventBus.Models.EventLogEntry>(){

                        new Hummingbird.Extersions.EventBus.Models.EventLogEntry("NewMsgEvent",new Hummingbird.WebApi.Events.NewMsgEvent{

                                Time=DateTime.Now
                            }),
                            new Hummingbird.Extersions.EventBus.Models.EventLogEntry("NewMsgEvent",new Hummingbird.WebApi.Events.NewMsgEvent(){
                                Time=DateTime.Now
                            })
                });
        }
    }
}
