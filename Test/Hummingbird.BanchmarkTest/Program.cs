using BenchmarkDotNet.Running;
using Hummingbird.Extersions.EventBus.SqlServerLogging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Hummingbird.BanchmarkTest
{
    class Program
    {
        static void Main(string[] args)
        {
            BenchmarkRunner.Run<EventLoggingBanchmarkTest>();
        }
    }
}
