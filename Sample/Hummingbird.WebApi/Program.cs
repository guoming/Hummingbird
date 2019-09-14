using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Hummingbird.Extensions.Configuration.Json;
using Microsoft.Extensions.DependencyInjection;
namespace Hummingbird.WebApi
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
                .ConfigureAppConfiguration((builderContext, config) =>
                  {
                      config.SetBasePath(Directory.GetCurrentDirectory());
                      config.AddJsonFileEx("appsettings.json");
                      config.AddJsonFileEx("cache.json");
                      config.AddEnvironmentVariables();
                  })
                .Build();
    }
}
