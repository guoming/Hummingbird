using Hummingbird.Example.Events;
using Hummingbird.Example.Events.CanalEvent;
using Hummingbird.Extensions.EventBus.Abstractions;
using Hummingbird.Extensions.EventBus.RabbitMQ;
using Hummingbird.Extensions.HealthChecks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;

namespace Hummingbird.Example
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;


        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddCors()
             .AddMvc(a => a.EnableEndpointRouting = false)
             .SetCompatibilityVersion(Microsoft.AspNetCore.Mvc.CompatibilityVersion.Version_3_0)
             .AddControllersAsServices();  //全局配置Json序列化处理



            services.AddHealthChecks(checks =>
            {
                checks.WithDefaultCacheDuration(TimeSpan.FromSeconds(5));
                checks.AddMySqlCheck("mysql", "Server=localhost;Port=3306;Database=example; User=root;Password=123456;pooling=True;minpoolsize=1;maxpoolsize=100;connectiontimeout=180;SslMode=None");
                checks.AddRedisCheck("redis", Configuration["redis:0:connectionString"]);
                checks.AddRabbitMQCheck("rabbitmq", factory =>
                {
                   factory.WithEndPoint(Configuration["EventBus:HostName"] ?? "localhost", int.Parse(Configuration["EventBus:Port"] ?? "5672"));
                   factory.WithAuth(Configuration["EventBus:UserName"] ?? "guest", Configuration["EventBus:Password"] ?? "guest");
                   factory.WithExchange(Configuration["EventBus:VirtualHost"] ?? "/");
                });
                checks.AddKafkaCheck("kafka", new Confluent.Kafka.ProducerConfig()
                {
                   Acks = Confluent.Kafka.Acks.All,
                   BootstrapServers = Configuration["Kafka:Sender:bootstrap.servers"]
                });
            });
            
            services.AddHummingbird(hummingbird =>
            {
                hummingbird.AddCanal(Configuration.GetSection("Canal"));
                hummingbird.AddResilientHttpClient((orign, option) =>
                 {
                     var setting = Configuration.GetSection("HttpClient");

                     if (!string.IsNullOrEmpty(orign))
                     {
                         var orginSetting = Configuration.GetSection($"HttpClient:{orign.ToUpper()}");
                         if (orginSetting.Exists())
                         {
                             setting = orginSetting;
                         }
                     }

                     option.DurationSecondsOfBreak = int.Parse(setting["DurationSecondsOfBreak"]);
                     option.ExceptionsAllowedBeforeBreaking = int.Parse(setting["ExceptionsAllowedBeforeBreaking"]);
                     option.RetryCount = int.Parse(setting["RetryCount"]);
                     option.TimeoutMillseconds = int.Parse(setting["TimeoutMillseconds"]);
                 })
                .AddCache(option =>
                {
                    option.ConfigName = "HummingbirdCache";
                    option.CacheRegion = Configuration["SERVICE_NAME"];
                })
                .AddDistributedLock((option) =>
                {
                    option.WithDb(0);
                    option.WithKeyPrefix("");
                    option.WithPassword(Configuration["Redis:Password"]);
                    option.WithServerList(Configuration["Redis:Server"]);
                    option.WithSsl(false);
                })
                .AddCacheing(option =>
                {
                    option.WithDb(0);
                    option.WithKeyPrefix("");
                    option.WithPassword(Configuration["Redis:Password"]);
                    option.WithReadServerList(Configuration["Redis:Server"]);
                    option.WithWriteServerList(Configuration["Redis:Server"]);

                    option.WithSsl(false);
                })
                .AddIdempotency(option =>
                {
                    option.Druation = TimeSpan.FromMinutes(5);
                    option.CacheRegion = "Idempotency";
                })
                .AddConsulDynamicRoute(Configuration, s =>
                 {
                     s.AddTags(Configuration["SERVICE_TAGS"]);
                 })
                .AddSnowflakeUniqueIdGenerator((workIdBuilder) =>
                {
                    workIdBuilder.CenterId = 0;
                    //workIdBuilder.AddStaticWorkIdCreateStrategy(1);
                    workIdBuilder.AddConsulWorkIdCreateStrategy(Configuration["SERVICE_NAME"]);

                })
                .AddOpenTracing(builder =>
                {
                    builder.AddJaeger(Configuration.GetSection("Tracing"));
                })
                .AddEventBus((builder) =>
                {
                    var Database_Server = Configuration["Database:SQLServer:Server"];
                    var Database_Database = Configuration["Database:SQLServer:Database"];
                    var Database_UserId = Configuration["Database:SQLServer:UserId"];
                    var Database_Password = Configuration["Database:SQLServer:Password"];
                    var DatabaseConnectionString = $"Server={Database_Server};Database={Database_Database};User Id={Database_UserId};Password={Database_Password};MultipleActiveResultSets=true";

                    builder

                    .AddMySqlEventLogging(o =>
                    {
                        o.WithEndpoint("Server=localhost;Port=63307;Database=test; User=root;Password=123456;pooling=True;minpoolsize=1;maxpoolsize=100;connectiontimeout=180");
                    })
                    //.AddSqlServerEventLogging(a =>
                    //{
                    //    a.WithEndpoint(DatabaseConnectionString);
                    //})
                    //.AddRabbitmq(factory =>
                    //{
                    //    factory.WithEndPoint(Configuration["EventBus:HostName"] ?? "localhost", int.Parse(Configuration["EventBus:Port"] ?? "5672"));
                    //    factory.WithAuth(Configuration["EventBus:UserName"] ?? "guest", Configuration["EventBus:Password"] ?? "guest");
                    //    factory.WithExchange(Configuration["EventBus:VirtualHost"] ?? "/");
                    //    factory.WithReceiver(PreFetch: 10, ReceiverMaxConnections: 1, ReveiverMaxDegreeOfParallelism: 1);
                    //    factory.WithSender(10);
                    //});
                    .AddKafka(option =>
                    {
                        option.WithSenderConfig(new Confluent.Kafka.ProducerConfig()
                        {
                            Acks = Confluent.Kafka.Acks.All,
                            BootstrapServers = Configuration["Kafka:Sender:bootstrap.servers"]
                        });
                        option.WithReceiverConfig(new Confluent.Kafka.ConsumerConfig()
                        {
                            EnableAutoOffsetStore = false,
                            EnableAutoCommit = false,
                            Acks = Confluent.Kafka.Acks.All,
                            AutoOffsetReset = Confluent.Kafka.AutoOffsetReset.Earliest,
                            GroupId = Configuration["Kafka:Receiver:GroupId"],                         
                            BootstrapServers = Configuration["Kafka:Receiver:bootstrap.servers"]
                        });
                        option.WithReceiver(
                            ReceiverAcquireRetryAttempts: 0,
                            ReceiverHandlerTimeoutMillseconds: 10000);

                        option.WithSender(
                            AcquireRetryAttempts: 3,
                            SenderConfirmTimeoutMillseconds: 1000,
                            SenderConfirmFlushTimeoutMillseconds: 20);
                    });


                });

                hummingbird.AddQuartz(Configuration.GetSection("Quartz"));

            });

        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            var eventBus = app.ApplicationServices.GetRequiredService<IEventBus>();
            var logger = app.ApplicationServices.GetRequiredService<ILogger<IEventLogger>>();
           
          

            app.UseHummingbird(humming =>
            {
                humming.UseEventBus(sp =>
                {
                    sp.UseSubscriber(eventbus =>
                    {
                        eventbus.RegisterBatch<TestEvent, TestEventHandler1>("TestEventHandler", "TestEventHandler");
                   
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

                            var events = obj.Messages.Select(message => message.WaitAndRetry(a => 5,3)).ToList();

                            var ret = !(await eventBus.PublishAsync(events));

                            return ret;
                        });
                    });
                });
                
            });
            
        }
    }
}