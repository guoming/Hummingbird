# Hummingbird

## 1.联系方式
> ![avatar](dingding.jpg)
> wechat：genius-ming
> email：geniusming@qq.com

## 2. 功能概要
* 分布式锁
  * 基于Redis
* 分布式缓存
  * 基于Redis
* 分布式Id
  * 基于Snowfake
* 分布式追踪 OpenTrack
  * 基于Jaeger
* 消息总线
    * 消息队列
      * 基于Rabbitmq
      * 基于Kafka
    * 消息可靠性保证
      * 基于MySql
      * 基于SqlServer
* 健康检查
  * Mongodb 健康检查
  * MySql 健康检查
  * Rabbitmq 健康检查
  * Redis 健康检查
  * SqlServer 健康检查
* 负载均衡
  * 随机负载均衡
  * 轮训负载均衡
* 服务动态路由
  * 基于Consul服务注册和发现
* 服务调用
  * 基于HTTP弹性客户端(支持：服务发现、负载均衡、超时、重试、熔断)
  * 基于HTTP非弹性客户端（支持：服务发现、负载均衡）
* Canal 数据集成
  * 输出到控制台
  * 输出到Rabbitmq(待实现)
  * 输出到Kafka(待实现)
  
## 3. 如何使用
### 3.1 分布式锁

步骤1：安装Nuget包
``` SHELL 
Install-Package Hummingbird.Extensions.DistributedLock -Version 1.15.0
```
步骤2：配置连接信息
``` C#
  public class Startup
  {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddHummingbird(hb =>
            {
                hb.AddDistributedLock(option =>
                 {
                     option.WithDb(0);
                     option.WithKeyPrefix("");
                     option.WithPassword("123456");
                     option.WithServerList("127.0.0.1:6379");
                     option.WithSsl(false);
                 });               
            });
        }
    }
```
步骤3：测试分布式锁
``` C#
    using Microsoft.AspNetCore.Mvc;
    using System;
    using System.Threading.Tasks;

    [Route("api/[controller]")]
    public class DistributedLockController : Controller
    {  private readonly IDistributedLock distributedLock;
        public DistributedLockController(IDistributedLock distributedLock)
        {
            this.distributedLock = distributedLock;
        } 

        [HttpGet]
        [Route("Test")]
        public async Task<string> Test()
        {
            var lockName = "name";
            var lockToken = Guid.NewGuid().ToString("N");
            try
            {
                if (distributedLock.Enter(lockName, lockToken, TimeSpan.FromSeconds(30)))
                {
                    // do something
                    return "ok";
                }
                else
                {
                    return "error";
                }
            }
            finally
            {
                distributedLock.Exit(lockName, lockToken);
            }
        }

    }

```
### 3.2 分布式缓存

步骤1：安装Nuget包
``` SHELL
Install-Package Hummingbird.Extensions.Cacheing -Version 1.15.0
```

步骤2：设置缓存连接信息
```C#
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
          
            services.AddHummingbird(hb =>
            {
                hb.AddCacheing(option =>
                {
                    option.WithDb(0);
                    option.WithKeyPrefix("");
                    option.WithPassword("123456");
                    option.WithReadServerList("192.168.109.44:6379");
                    option.WithWriteServerList("192.168.109.44:6379");
                    option.WithSsl(false);
                })

            });

        }
    }
```

步骤3：测试Redis缓存
```c#
    using Hummingbird.Extensions.Cacheing;
    using Microsoft.AspNetCore.Mvc;
    using System.Threading.Tasks;

    [Route("api/[controller]")]
    public class CacheingController : Controller
    {
        private readonly ICacheManager cacheManager;
        public CacheingController(ICacheManager cacheManager)
        {
            this.cacheManager = cacheManager;
        }

        [HttpGet]
        [Route("Test")]
        public async Task<string> Test()
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
```
### 3.3 分布式Id

步骤1： 安装Nuget包
``` SHELL
  Install-Package Hummingbird.Extensions.UidGenerator -Version 1.15.5
  Install-Package Hummingbird.Extensions.UidGenerator.ConsulWorkIdStrategy -Version 1.15.7
```

步骤2：配置使用Snowfake算法生产唯一Id
```c#
  public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {          
            services.AddHummingbird(hb =>
            {
                hb.AddSnowflakeUniqueIdGenerator(workIdBuilder =>
                {
                    workIdBuilder.CenterId = 0; // 设置CenterId
                    workIdBuilder.AddConsulWorkIdCreateStrategy("Example"); //设置使用Consul创建WorkId
                })
            });
        }
    }
```
步骤3：测试Id生成
```c#
    using Hummingbird.Extensions.UidGenerator;
    using Microsoft.AspNetCore.Mvc;
    using System.Threading.Tasks;

   [Route("api/[controller]")]
    public class UniqueIdController : Controller
    {
        private readonly IUniqueIdGenerator uniqueIdGenerator;

        public UniqueIdController(IUniqueIdGenerator uniqueIdGenerator)
        {
            this.uniqueIdGenerator = uniqueIdGenerator;
        }

        [HttpGet]
        [Route("Test")]
        public async Task<long> Test()
        {
            return uniqueIdGenerator.NewId();
        }
    }
```
### 3.4 分布式追踪

步骤1：安装Nuget包
``` SHELL
Install-Package Hummingbird.Extensions.OpenTracing -Version 1.15.0
Install-Package Hummingbird.Extensions.OpenTracking.Jaeger -Version 1.15.0
```
步骤2： 创建tracing.json 配置
```JSON
 {
  "Tracing": {
    "Open": false,
    "SerivceName": "SERVICE_EXAMPLE",
    "FlushIntervalSeconds": 15,
    "SamplerType": "const",
    "LogSpans": true,
    "AgentPort": "5775", //代理端口
    "AgentHost": "dev.jaeger-agent.service.consul", //代理地址
    "EndPoint": "http://dev.jaeger-collector.service.consul:14268/api/traces"
  }
}
```

步骤3：添加tracing.json 配置依赖
``` C#

    public class Program
    {
        public static void Main(string[] args)
        {
            BuildWebHost(args).Run();
        }

        public static IWebHost BuildWebHost(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>()                
                .ConfigureAppConfiguration((builderContext, config) =>
                  {
                      config.SetBasePath(Directory.GetCurrentDirectory());
                      config.AddJsonFile("tracing.json");
                      config.AddEnvironmentVariables();
                  })
           .ConfigureLogging((hostingContext, logging) =>
           {
               logging.ClearProviders();
        
           })
           .Build();
    }
```

步骤3： 配置OpenTracing基于Jaeger实现
```c#
   public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddHummingbird(hb =>
            {
                hb.AddOpenTracing(builder => {
                    builder.AddJaeger(Configuration.GetSection("Tracing"));
                })
            });
        }
    }
```

步骤4：测试手动埋点日志
```c#
    using Microsoft.AspNetCore.Mvc;
    using System.Threading.Tasks;

    [Route("api/[controller]")]
    public class OpenTracingController : Controller
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
```
### 3.5 消息总线

步骤1：安装Nuget包
``` SHELL
Install-Package Hummingbird.Extensions.EventBus -Version 1.15.0
Install-Package Hummingbird.Extensions.EventBus.RabbitMQ -Version 1.15.3
Install-Package Hummingbird.Extensions.EventBus.MySqlLogging -Version 1.15.3
```

步骤2：创建消息消费端，消息处理程序
```c#

  using Hummingbird.Extensions.EventBus.Abstractions;
  using System.Collections.Generic;
  using System.Threading;
  using System.Threading.Tasks;

  public class TestEvent
  {
        public string EventType { get; set; }
  }

  public class TestEventHandler1 : IEventHandler<TestEvent>
  {
        public Task<bool> Handle(TestEvent @event, Dictionary<string, object> headers, CancellationToken cancellationToken)
        {
            //执行业务操作1并返回操作结果
            return Task.FromResult(true);
        }
    }

    public class TestEventHandler2 : IEventHandler<TestEvent>
    {
        public Task<bool> Handle(TestEvent @event, Dictionary<string, object> headers, CancellationToken cancellationToken)
        {
            //执行业务操作2并返回操作结果
            return Task.FromResult(true);
        }
    }
```

步骤2：创建消息生产端，消息发送程序
```c# 
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
    public class MQPublisherTestController : Controller
    {
        private readonly IEventLogger eventLogger;
        private readonly IEventBus eventBus;

        public MQPublisherTestController(
            IEventLogger eventLogger,
            IEventBus eventBus)
        {
            this.eventLogger = eventLogger;
            this.eventBus = eventBus;
        }

        /// <summary>
        ///  无本地事务发布消息，消息直接写入队列
        /// </summary>
        [HttpGet]
        [Route("Test1")]
        public async Task<string> Test1()
        {
            var events = new List<EventLogEntry>() {
                   new EventLogEntry("TestEvent",new Events.TestEvent() {
                      EventType="Test1"
                   }),
                   new EventLogEntry("TestEvent",new {
                        EventType="Test1"
                   }),
            };

            var ret=  await  eventBus.PublishAsync(events);

            return ret.ToString();
        }

        /// <summary>
        /// 有本地事务发布消息，消息落盘到数据库确保事务完整性
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("Test2")]
        public async Task<string> Test2()
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

        /// <summary>
        /// 有本地事务发布消息，消息落盘到数据库，从数据库重新取出消息发送到队列
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("Test3")]
        public async Task Test3()
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
```

步骤3： 配置使用Rabbitmq消息队列和使用Mysql消息持久化
```c#
   public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddHummingbird(hb =>
            {
                hb.AddEventBus((builder) =>
                {
                    //使用Rabbitmq 消息队列
                    builder.AddRabbitmq(factory =>
                    {
                        factory.WithEndPoint("192.168.109.2,192.168.109.3", "5672"));
                        factory.WithAuth("guest", "guest");
                        factory.WithExchange("/");
                        factory.WithReceiver(PreFetch: 10, ReceiverMaxConnections: 1, ReveiverMaxDegreeOfParallelism: 1);
                        factory.WithSender(10);
                    });
                    //使用Kafka 消息队列
                    //builder.AddKafka(option =>
                    //{
                    //    option.WithSenderConfig(new Confluent.Kafka.ProducerConfig()
                    //    {

                    //        EnableDeliveryReports = true,
                    //        BootstrapServers = "192.168.78.29:9092,192.168.78.30:9092,192.168.78.31:9092",
                    //        // Debug = "msg" //  Debug = "broker,topic,msg"
                    //    });

                    //    option.WithReceiverConfig(new Confluent.Kafka.ConsumerConfig()
                    //    {
                    //        // Debug= "consumer,cgrp,topic,fetch",
                    //        GroupId = "test-consumer-group",
                    //        BootstrapServers = "192.168.78.29:9092,192.168.78.30:9092,192.168.78.31:9092",
                    //    });
                    //    option.WithReceiver(1, 1);
                    //    option.WithSender(10, 3, 1000 * 5, 50);
                    //});

                    // 基于MySql 数据库 进行消息持久化，当存在分布式事务问题时
                    builder.AddMySqlEventLogging(o => {
                        o.WithEndpoint("Server=localhost;Port=63307;Database=test; User=root;Password=123456;pooling=True;minpoolsize=1;maxpoolsize=100;connectiontimeout=180");
                    });

                    // 基于SqlServer 数据库 进行消息持久化，当存在分布式事务问题时
                    //builder.AddSqlServerEventLogging(a =>
                    //{
                    //     a.WithEndpoint("Data Source=localhost,63341;Initial Catalog=test;User Id=sa;Password=123456");
                    //});
                 
                })
             

            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            var eventBus = app.ApplicationServices.GetRequiredService<IEventBus>();
            var logger = app.ApplicationServices.GetRequiredService<ILogger<IEventLogger>>();

            app.UseHummingbird(humming =>
            {
              
                humming.UseEventBus(sp =>
                {
                    sp.UseSubscriber(eventbus =>
                    {
                        eventbus.Register<TestEvent, TestEventHandler1>("TestEventHandler1", "TestEvent");
                        eventbus.Register<TestEvent, TestEventHandler2>("TestEventHandler2", "TestEvent");
   
                        //订阅消息
                        eventbus.Subscribe((Messages) =>
                        {
                            foreach (var message in Messages)
                            {
                                logger.LogDebug($"ACK: queue {message.QueueName} route={message.RouteKey} messageId:{message.MessageId}");
                            }

                        }, async (obj) =>
                        {                         
                            foreach (var message in obj.Messages)
                            {
                                logger.LogError($"NAck: queue {message.QueueName} route={message.RouteKey} messageId:{message.MessageId}");
                            }

                            //消息消费失败执行以下代码
                            if (obj.Exception != null)
                            {
                                logger.LogError(obj.Exception, obj.Exception.Message);
                            }

                            // 消息等待5秒后重试，最大重试次数3次
                            var events = obj.Messages.Select(message => message.WaitAndRetry(a => 5,3)).ToList();

                            // 消息写到重试队列
                            var ret = !(await eventBus.PublishAsync(events));

                            return ret;
                        });
                    });
                });

            });

        }
    }
```


### 3.6 健康检查
步骤1： 安装Nuget包
``` SHELL
Install-Package Hummingbird.Extensions.HealthChecks -Version 1.15.0
Install-Package Hummingbird.Extensions.HealthChecks.Redis -Version 1.15.0
Install-Package Hummingbird.Extensions.HealthChecks.Rabbitmq -Version 1.15.0
Install-Package Hummingbird.Extensions.HealthChecks.MySql -Version 1.15.0
Install-Package Hummingbird.Extensions.HealthChecks.SqlServer -Version 1.15.0
```

步骤2： 配置健康检查Endpoint
```c#
    public class Program
    {
        public static void Main(string[] args)
        {
            BuildWebHost(args).Run();           
        }

        public static IWebHost BuildWebHost(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>()
                .UseHealthChecks("/healthcheck")
                .ConfigureAppConfiguration((builderContext, config) =>
                  {
                      config.SetBasePath(Directory.GetCurrentDirectory());                 
                      config.AddEnvironmentVariables();
                  })
           .ConfigureLogging((hostingContext, logging) =>
           {
               logging.ClearProviders();
        
           })
          .Build();
    }
```

步骤3： 配置监控检查项
```c#   
    public class Startup
    {
        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddHealthChecks(checks =>
            {
                checks.WithDefaultCacheDuration(TimeSpan.FromSeconds(5));
                checks.AddMySqlCheck("mysql", "Server=localhost;Port=63307;Database=test; User=root;Password=123456;pooling=True;minpoolsize=1;maxpoolsize=100;connectiontimeout=180;SslMode=None");
                checks.AddSqlCheck("sqlserver", "Data Source=localhost,63341;Initial Catalog=test;User Id=sa;Password=123456");
                checks.AddRedisCheck("redis", "localhost:6379,password=123456,allowAdmin=true,ssl=false,abortConnect=false,connectTimeout=5000");
                checks.AddRabbitMQCheck("rabbitmq", factory =>
                {
                    factory.WithEndPoint("192.168.109.2,192.168.109.3","5672"));
                    factory.WithAuth("guest", "guest");
                    factory.WithExchange("/");
                });
            });
        }
    }
```


### 3.8 服务注册 + 服务发现 + 服务HTTP调用

步骤1： 安装Nuget包
``` SHELL
Install-Package Hummingbird.DynamicRoute -Version 1.15.1
Install-Package Hummingbird.LoadBalancers -Version 1.15.0
Install-Package Hummingbird.Extensions.DynamicRoute.Consul -Version 1.15.5
Install-Package Hummingbird.Extensions.Resilience.Http -Version 1.15.0
```

步骤2：配置 appsettings.json
```JSON
{
  "SERVICE_REGISTRY_ADDRESS": "localhost", // 注册中心地址
  "SERVICE_REGISTRY_PORT": "8500", //注册中心端口
  "SERVICE_SELF_REGISTER": true, //自注册开关打开
  "SERVICE_NAME": "SERVICE_EXAMPLE", //服务名称
  "SERVICE_ADDRESS": "",
  "SERVICE_PORT": "80",
  "SERVICE_TAGS": "test",
  "SERVICE_REGION": "DC1",
  "SERVICE_80_CHECK_HTTP": "/healthcheck",
  "SERVICE_80_CHECK_INTERVAL": "15",
  "SERVICE_80_CHECK_TIMEOUT": "15",
  "SERVICE_CHECK_TCP": null,
  "SERVICE_CHECK_SCRIPT": null,
  "SERVICE_CHECK_TTL": "15",
  "SERVICE_CHECK_INTERVAL": "5",
  "SERVICE_CHECK_TIMEOUT": "5"
}
```

步骤3：添加appsettings.json 配置依赖

``` C#

    public class Program
    {
        public static void Main(string[] args)
        {
            BuildWebHost(args).Run();
        }

        public static IWebHost BuildWebHost(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>()                
                .ConfigureAppConfiguration((builderContext, config) =>
                  {
                      config.SetBasePath(Directory.GetCurrentDirectory());
                      config.AddJsonFile("appsettings.json");
                      config.AddEnvironmentVariables();
                  })
           .ConfigureLogging((hostingContext, logging) =>
           {
               logging.ClearProviders();
        
           })
           .Build();
    }
```

步骤4：服务注册到Consul并配置弹性HTTP客户端
```c#   
    public class Startup
    {

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddHummingbird(hummingbird =>
            {
                hummingbird                
                // 服务注册到Consul
                .AddConsulDynamicRoute(Configuration, s =>
                 {
                     s.AddTags("version=v1");
                 })
                 // 设置弹性HTTP客户端（服务发现、超时、重试、熔断）
                .AddResilientHttpClient((orign, option) =>
                 {
                     var setting = Configuration.GetSection("HttpClient");

                     if (!string.IsNullOrEmpty(orign))
                     {
                         var orginSetting = Configuration.GetSection($"HttpClient:{orign.ToUpper()}");
                         if(orginSetting.Exists())
                         {
                             setting = orginSetting;
                         }
                     }

                     option.DurationSecondsOfBreak = int.Parse(setting["DurationSecondsOfBreak"]);
                     option.ExceptionsAllowedBeforeBreaking = int.Parse(setting["ExceptionsAllowedBeforeBreaking"]);
                     option.RetryCount = int.Parse(setting["RetryCount"]);
                     option.TimeoutMillseconds = int.Parse(setting["TimeoutMillseconds"]);

                 });
               
            });
        }
    }
```

步骤5：测试HTTP Client
```c#   
    using Hummingbird.Extensions.Resilience.Http;
    using Microsoft.AspNetCore.Mvc;
    using System.Threading;
    using System.Threading.Tasks;
    [Route("api/[controller]")]
    public class HttpClientTestController : Controller
    {
        private readonly IHttpClient httpClient;
        public HttpClientTestController(IHttpClient httpClient)
        {
            this.httpClient = httpClient;
        }
      

        [HttpGet]
        [Route("Test1")]
        public async Task<string> Test1()
        {
            return await httpClient.GetStringAsync("http://baidu.com");
        }

        [HttpGet]
        [Route("Test2")]
        public async Task<string> Test2()
        {
            return await (await httpClient.PostAsync(
                uri: "http://{SERVICE_EXAMPLE}/healthcheck",
                item: new { },
                authorizationMethod: null, 
                authorizationToken: null,
                dictionary: null,
                cancellationToken: CancellationToken.None)).Content.ReadAsStringAsync();
        }

    }
```


### 3.8 Canal 数据集成

步骤1： 安装Nuget包
``` SHELL
Install-Package Hummingbird.Extensions.Canal -Version 1.0.0
```

步骤2：配置 canal.json, binlog日志输出到控制台
```JSON
{
 "Canal": {
    "Subscribes": [
      {
        "Filter": ".*\\..*",
        "BatchSize": 1024,
        "Format": "Hummingbird.Extensions.Canal.Formatters.CanalJson.Formatter,Hummingbird.Extensions.Canal", //MaxwellJsonFormatter,CanalJsonFormatter
        "Connector": "Hummingbird.Extensions.Canal.Connectors.ConsoleConnector,Hummingbird.Extensions.Canal",
        "ConnectionInfo": {
          "Address": "192.168.87.125",
          "Port": 11111,
          "Destination": "test1",
          "UserName": "",
          "Passsword": ""
        }
      }
    ]
  }
```

步骤3：添加appsettings.json 配置依赖
``` C#

    public class Program
    {
        public static void Main(string[] args)
        {
            BuildWebHost(args).Run();
        }

        public static IWebHost BuildWebHost(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>()                
                .ConfigureAppConfiguration((builderContext, config) =>
                  {
                      config.SetBasePath(Directory.GetCurrentDirectory());
                      config.AddJsonFile("canal.json");
                      config.AddEnvironmentVariables();
                  })
           .Build();
    }
```

步骤4： 实现自己的binlog处理
``` C#
    public class ConsoleSubscripter : ISubscripter
    {
        public bool Process(CanalEventEntry[] entrys)
        {
            foreach(var entry in entrys)
            {
                Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(entry));
            }

            //ack
            return true;
        }
    }
```


