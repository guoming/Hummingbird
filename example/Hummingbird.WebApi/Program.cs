using System.IO;
using Hummingbird.AspNetCore.HealthChecks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Hummingbird.Example
{
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
                .UseMetrics((builderContext, metricsBuilder) => {
                    metricsBuilder.ToPrometheus();
                    metricsBuilder.ToInfluxDb(builderContext.Configuration.GetSection("AppMetrics:Influxdb"));
                })
                .ConfigureAppConfiguration((builderContext, config) =>
                  {
                      config.SetBasePath(Directory.GetCurrentDirectory());
                      config.AddJsonFile("Config/appsettings.json");
                      config.AddJsonFile("Config/cache.json");
                      config.AddJsonFile("Config/tracing.json");
                      config.AddEnvironmentVariables();
                  })
               .ConfigureLogging((hostingContext, logging) =>
               {
                   logging.ClearProviders();
                   logging.AddConsole();
                   

               })
               .Build();
    }
}
