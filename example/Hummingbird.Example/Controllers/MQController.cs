

namespace Hummingbird.Example.Controllers
{
    using Hummingbird.Extensions.EventBus.Abstractions;
    using Hummingbird.Extensions.EventBus.Models;
    using Microsoft.AspNetCore.Mvc;
    using MySql.Data.MySqlClient;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Dapper;
    using System.Linq;
    using System.Threading;
    [Route("api/[controller]")]
    public class MQController : Controller
    {
        private readonly IEventLogger eventLogger;
        private readonly IEventBus eventBus;

        public MQController(
            IEventLogger eventLogger,
            IEventBus eventBus)
        {
            this.eventLogger = eventLogger;
            this.eventBus = eventBus;
        }

   

        [HttpGet]
        [Route("PublishEvents")]
        public async Task<string> PublishAsync()
        {
            var item1 = new EventLogEntry("TestEventHandler", new Events.TestEvent()
            {
                EventType = "Test1",


            });
            var item2 = new EventLogEntry("TestEventHandler",0, new Events.TestEvent()
            {
                EventType = "Test1",
               
                
            });
            item2.EventId = 1;

            var item3 = new EventLogEntry("TestEventHandler", 1, new
            {

                EventType = "Test2"
            });


            var events = new List<EventLogEntry>() {
                   item1,item2,item3
            };

            var ret=  await  eventBus.PublishAsync(events);

            return ret.ToString();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("SaveEvents")]
        public async Task<string> SaveEvents()
        {   
            var connectionString = "Server=localhost;Port=63307;Database=test; User=root;Password=123456;pooling=True;minpoolsize=1;maxpoolsize=100;connectiontimeout=180";

            using (var sqlConnection = new MySqlConnection(connectionString))
            {
                await sqlConnection.OpenAsync();
                
                var sqlTran = await sqlConnection.BeginTransactionAsync();

                var events = new List<EventLogEntry>() {
                   new EventLogEntry("TestEvent",new Events.TestEvent() {
                      EventType="Test2"
                   }),
                   new EventLogEntry("TestEvent",new {
                        EventType="Test2"
                   }),
            };

                //保存消息至业务数据库，保证写消息和业务操作在一个事务
                await eventLogger.SaveEventAsync(events, sqlTran);

                var ret = await sqlConnection.ExecuteAsync("you sql code");

                return ret.ToString();
            }
        }

        [HttpGet]
        [Route("PublishSavedEvents")]
        public async Task PublishSavedEvents()
        {   
            //获取1000条没有发布的事件
            var unPublishedEventList = eventLogger.GetUnPublishedEventList(1000);
            //通过消息总线发布消息
            var ret = await eventBus.PublishAsync(unPublishedEventList);

            if (ret)
            {
                await eventLogger.MarkEventAsPublishedAsync(unPublishedEventList.Select(a => a.EventId).ToList(), CancellationToken.None);
            }
            else
            {
                await eventLogger.MarkEventAsPublishedFailedAsync(unPublishedEventList.Select(a => a.EventId).ToList(), CancellationToken.None);
            }
        }
    }

}
