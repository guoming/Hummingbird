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
using System.Net.Http;
using Confluent.Kafka;
using Hummingbird.Extensions.FileSystem.Oss.StaticFile;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;
using PhysicalFileProvider = Hummingbird.Extensions.FileSystem.Physical.PhysicalFileProvider;

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
                HttpClientHandler clientHandler = new HttpClientHandler();
                clientHandler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => { return true; };
                
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
                     
                 },clientHandler) 
                   .AddCanal(Configuration.GetSection("Canal"))
                // .AddCache(option =>
                // {
                //     option.ConfigName = "HummingbirdCache";
                //     option.CacheRegion = Config["SERVICE_NAME"];
                // })
                 .AddRedisDistributedLock((option) =>
                {
                    option.WithDb(0);
                    option.WithKeyPrefix("");
                    option.WithPassword(Configuration["Redis:Password"]);
                    option.WithServerList(Configuration["Redis:Server"]);
                    option.WithSsl(false);
                    option.WithLockExpirySeconds(30);
                })
                //.AddConsulDistributedLock(Config)
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
                //.AddNacosDynamicRoute(Config.GetSection("Nacos"))
                .AddConsulDynamicRoute(Configuration, s =>
                 {
                     s.AddTags(Configuration["SERVICE_TAGS"]);
                 })
                .AddSnowflakeUniqueIdGenerator((workIdBuilder) =>
                   { 
                       var CenterId = 0;
                       //workIdBuilder.AddStaticWorkIdCreateStrategy(CenterId,1);
                        //workIdBuilder.AddHostNameWorkIdCreateStrategy(CenterId);
                        workIdBuilder.AddConsulWorkIdCreateStrategy(CenterId,Configuration["SERVICE_NAME"]);
                   })
                .AddOpenTracing(builder =>
                {
                    builder.AddJaeger(Configuration.GetSection("Jaeger"));
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
                            o.WithEndpoint(
                                "Server=localhost;Port=63307;Database=test; User=root;Password=123456;pooling=True;minpoolsize=1;maxpoolsize=100;connectiontimeout=180");
                        })
                        //.AddSqlServerEventLogging(a =>
                        //{
                        //    a.WithEndpoint(DatabaseConnectionString);
                        //})

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
                    // .AddRabbitmq(factory =>
                    // {
                    //     factory.WithEndPoint(Config["EventBus:HostName"] ?? "localhost",
                    //         int.Parse(Config["EventBus:Port"] ?? "5672"));
                    //     factory.WithAuth(Config["EventBus:UserName"] ?? "guest",
                    //         Config["EventBus:Password"] ?? "guest");
                    //     factory.WithExchange(Config["EventBus:VirtualHost"] ?? "/");
                    //     factory.WithReceiver(PreFetch: 10, ReceiverMaxConnections: 1,
                    //         ReveiverMaxDegreeOfParallelism: 1);
                    //     factory.WithSender(10);
                    // });
                });
                
                hummingbird.AddQuartz(Configuration.GetSection("Quartz"));
                hummingbird.AddOssFileSystem(Configuration.GetSection("FileSystem:Oss"));
              //  hummingbird.AddPhysicalFileSystem(Configuration.GetSection("FileSystem:Physical"));

            });

        }
        
        

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env,IHttpContextAccessor accessor)
        {
            HttpContextProvider.Accessor = accessor;
            
            var eventBus = app.ApplicationServices.GetRequiredService<IEventBus>();
            var logger = app.ApplicationServices.GetRequiredService<ILogger<IEventLogger>>();

            app.UseMvc();
            
            var contentTypeProvider = app.ApplicationServices.GetRequiredService<IContentTypeProvider>();
            var fileProvider = app.ApplicationServices.GetRequiredService<IFileProvider>();
            var staticFileOptions = new StaticFileOptions
            {
                FileProvider = fileProvider,
                RequestPath = "",
                ContentTypeProvider = contentTypeProvider
            };
            app.UseStaticFiles(staticFileOptions);
            app.UseOssStaticFiles(staticFileOptions);

            app.UseHummingbird(humming =>
            {
                humming.UseEventBus(sp =>
                {
                    sp.UseSubscriber(eventbus =>
                    {
                        //eventbus.RegisterBatch<TestEvent, TestEventHandler1>("TestEventHandler", "TestEventHandler");
                   
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