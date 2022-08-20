using System;
using System.Collections.Generic;
using System.IO;
using Com.Ctrip.Framework.Apollo.Enums;
using Com.Ctrip.Framework.Apollo.Logging;
using Hummingbird.AspNetCore.HealthChecks;
using Jaeger;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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
                .UseShutdownTimeout(TimeSpan.FromSeconds(30))
                .UseStartup<Startup>()
                
                .UseHealthChecks("/healthcheck")
                .UseMetrics((builderContext, metricsBuilder) => {
                    metricsBuilder.ToPrometheus();
                   metricsBuilder.ToInfluxDb(builderContext.Configuration.GetSection("AppMetrics:Influxdb"));
                })
                .ConfigureAppConfiguration((builderContext, config) =>
                { 
                    config.SetBasePath(Directory.GetCurrentDirectory()); 
                    config.AddEnvironmentVariables();
                    config.AddCommandLine(args);
                    config.AddJsonFileEx("Config/bootstrap.json",false,true);
                    config.AddJsonFileEx($"Config/appsettings-{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")}.json",true,true);
                    
                    var configuration = config.Build();
                    config.AddNacosConfiguration(configuration.GetSection("Nacos"));
                    config.AddApolloConfiguration(configuration.GetSection("Apollo"),
                        new Dictionary<string, ConfigFileFormat>()
                        {
                            { "appsettings", ConfigFileFormat.Json }
                        });
                })
               .ConfigureLogging((hostingContext, logging) =>
               {
                   logging.ClearProviders();
                   //logging.AddConfiguration(hostingContext.Configuration.GetSection("Logging"));
                   //logging.AddLog4Net("Config/log4net.xml", true);
                   logging.AddConsole();
                   logging.AddDebug();
                   LogManager.UseConsoleLogging(Com.Ctrip.Framework.Apollo.Logging.LogLevel.Debug);
               

               })
               .Build();
    }
}
