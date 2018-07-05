using Hummingbird.Extersions.EventBus.SqlServerLogging;
using System;

namespace Hummingbird.BanchmarkTest
{
    class Program
    {
        static void Main(string[] args)
        {
            EventLoggingBanchmarkTest eventLoggingBanchmarkTest = new EventLoggingBanchmarkTest();
            eventLoggingBanchmarkTest.Run();
        }
    }
}
