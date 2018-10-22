using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Hummingbird.WebApi.Events;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

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
                .AddIdempotency(option =>
                {
                    option.Druation = TimeSpan.FromMinutes(5);
                    option.CacheRegion = "Idempotency";
                })
                .AddServiceRegistry(s =>
                {                   
                    s.SERVICE_REGISTRY_ADDRESS = Configuration["SERVICE_REGISTRY_ADDRESS"];
                    s.SERVICE_REGISTRY_PORT = Configuration["SERVICE_REGISTRY_PORT"];
                    s.SERVICE_SELF_REGISTER = Configuration["SERVICE_SELF_REGISTER"];
                    s.SERVICE_ADDRESS = Configuration["SERVICE_ADDRESS"];
                    s.SERVICE_NAME = Configuration["SERVICE_NAME"];
                    s.SERVICE_PORT = Configuration["SERVICE_PORT"];
                    s.SERVICE_REGION = Configuration["SERVICE_REGION"];
                    s.SERVICE_80_CHECK_HTTP = Configuration["SERVICE_80_CHECK_HTTP"];
                    s.SERVICE_80_CHECK_INTERVAL = Configuration["SERVICE_80_CHECK_INTERVAL"];
                    s.SERVICE_80_CHECK_TIMEOUT = Configuration["SERVICE_80_CHECK_TIMEOUT"];
                    s.SERVICE_TAGS = Configuration["SERVICE_TAGS"];
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
                        factory.HostName = Configuration["EventBus:HostName"] ?? "localhost";
                        factory.Port = int.Parse(Configuration["EventBus:Port"] ?? "5672");
                        factory.UserName = Configuration["EventBus:UserName"] ?? "guest";
                        factory.Password = Configuration["EventBus:Password"] ?? "guest";
                        factory.VirtualHost = Configuration["EventBus:VirtualHost"] ?? "/";
                        factory.RetryCount = int.Parse(Configuration["EventBus:RetryCount"] ?? "3");
                    })
                    .AddSqlServerEventLogging(DatabaseConnectionString);



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
                humming.UseEventBus(async sp =>
                {
                    sp.UseSubscriber(eventbus =>
                    {
                        eventbus.RegisterBatch<Events.NewMsgEvent, Events.NewMsgEventHandler>();
                    });
                    try
                    {
                        await sp.UseDispatcherAsync(1000);
                    }
                    catch
                    { }
                });

            });
            app.UseMvc();
        }
    }
}
