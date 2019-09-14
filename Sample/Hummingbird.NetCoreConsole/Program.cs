using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.IO;

namespace Hummingbird.NetCoreConsole
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }
    
        public void Configure(Microsoft.AspNetCore.Builder.IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {

            app.UseHummingbird(humming =>
            {
                humming.UseServiceRegistry(s =>
                {
                    s.WithConfig(Configuration.Get<Extersions.ServiceRegistry.ServiceConfig>());
                });
            });
        }
    }
    class Program
    {
        public static IHostBuilder CreateHostBuilder(string[] args) =>
            new HostBuilder()           
                .ConfigureAppConfiguration((hostingContext, config) =>
                {
                    config.AddJsonFile("appsettings.json");

                })
                .ConfigureServices((hostContext, services) =>
                {
                    #region  services

                    services.AddTransient<IConfiguration>(a => hostContext.Configuration);

                  
                    #endregion

                })

                .ConfigureLogging((hostingContext, logging) =>
                {
               

#if DEBUG
                    logging.AddDebug();
#endif
                })
                .UseConsoleLifetime();


        static void Main(string[] args)
        {
            var host = CreateHostBuilder(args).Build();
     
            host.Run();
        }
    }
}