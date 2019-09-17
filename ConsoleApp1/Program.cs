using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.IO;

namespace ConsoleApp1
{
    class Program
    {
        public static IHostBuilder CreateHostBuilder(string[] args) =>
                new HostBuilder()
               .ConfigureAppConfiguration((hostingContext, config) =>
                {
                    config.SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json", false, true);
                })
                .ConfigureServices((hostContext, services) =>
                {
                    
                    #region  services

                    services.AddServiceRegisterHostedService(hostContext.Configuration);

                    #endregion

                })

                .UseConsoleLifetime();


        static void Main(string[] args)
        {
            var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            Environment.SetEnvironmentVariable("Environment", env);

            if (!new List<string>() { "dev", "test", "sbx", "qss", "live" }.Contains(env))
            {
                throw new ArgumentException("环境变量 ASPNETCORE_ENVIRONMENT 必须是 dev/test/sbx/qss/live");
            }


            var host = CreateHostBuilder(args).UseEnvironment(env).Build();
            


            host.Run();
        }
    }
}
