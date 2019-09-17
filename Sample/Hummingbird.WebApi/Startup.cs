using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Hummingbird.WebApi
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
            services.AddMvc();

            services.AddServiceRegisterHostedService(Configuration);
            services.AddHummingbird(hummingbird =>
            {
                hummingbird                
                 .AddResilientHttpClient(option =>
                 {
                     option.DurationSecondsOfBreak = int.Parse(Configuration["HttpClient:DurationSecondsOfBreak"]);
                     option.ExceptionsAllowedBeforeBreaking = int.Parse(Configuration["HttpClient:ExceptionsAllowedBeforeBreaking"]);
                     option.RetryCount = int.Parse(Configuration["HttpClient:RetryCount"]);
                     option.TimeoutMillseconds = int.Parse(Configuration["HttpClient:TimeoutMillseconds"]);
                 })
                .AddCache(option =>
                {
                    option.ConfigName = "HummingbirdCache";
                    option.CacheRegion = Configuration["SERVICE_NAME"];
                })
                .AddCacheing(option => {

                    option.WithDb(0);
                    option.WithKeyPrefix("");
                    option.WithPassword("123456");
                    option.WithReadServerList("192.168.109.44:6379");
                    option.WithWriteServerList("192.168.109.44:6379");
                    option.WithSsl(false);
                })
                .AddIdempotency(option =>
                {
                    option.Druation = TimeSpan.FromMinutes(5);
                    option.CacheRegion = "Idempotency";
                })
          
                .AddUniqueIdGenerator(IdGenerator =>
                {
                    IdGenerator.CenterId = 0;
                    IdGenerator.UseStaticWorkIdCreateStrategy(0);
                })
                .AddEventBus((builder) =>
                {
                    var Database_Server = Configuration["Database:SQLServer:Server"];
                    var Database_Database = Configuration["Database:SQLServer:Database"];
                    var Database_UserId = Configuration["Database:SQLServer:UserId"];
                    var Database_Password = Configuration["Database:SQLServer:Password"];
                    var DatabaseConnectionString = $"Server={Database_Server};Database={Database_Database};User Id={Database_UserId};Password={Database_Password};MultipleActiveResultSets=true";

                    builder
                    .AddRabbitmq(factory =>
                    {
                        factory.WithEndPoint(Configuration["EventBus:HostName"] ?? "localhost", int.Parse(Configuration["EventBus:Port"] ?? "5672"));
                        factory.WithAuth(Configuration["EventBus:UserName"] ?? "guest", Configuration["EventBus:Password"] ?? "guest");
                        factory.WithExchange(Configuration["EventBus:VirtualHost"] ?? "/");
                        factory.WithReceiver();
                        factory.WithSender(10);
                    });             
                    //.AddSqlServerEventLogging(a =>
                    // {
                    //     a.WithEndpoint(DatabaseConnectionString);
                    // });
                });

            });


        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
           
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHummingbird(humming =>
            {
                humming.UseServiceRegistry(s =>
                {
                    s.WithConfig(Configuration.Get<Extersions.ServiceRegistry.ServiceConfig>());
                });
                humming.UseEventBus(sp =>
                {
                    sp.UseSubscriber(eventbus =>
                    {
                        //eventbus.RegisterBatch<Events.NewMsgEvent, Events.NewMsgEventBatchHandler>("NewMsgEventBatchHandler", "NewMsgEvent");
                        //eventbus.Register<NewMsgEvent, NewMsgEventHandler>("NewMsgEventHandler", "NewMsgEvent");
                        //eventbus.RegisterBatch<ChangeDataCaptureEvent, ChangeDataCaptureEventToESIndexHandler>("", "#");
                    });
                });

            });
            app.UseMvc();

            var ip = Configuration["Ip"];

        }
    }
}