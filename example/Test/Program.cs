using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Test
{
    class Program
    {
        static void Main(string[] args)
        {

            var cache = Hummingbird.Extensions.Cacheing.CacheFactory.Build(option => {

                option.WithReadServerList("127.0.0.1:6379");
                option.WithWriteServerList("127.0.0.1:6379");
                option.WithDb(0);
                option.WithNumberOfConnections(1);
            });

            System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
            stopwatch.Start();
            var tasks = new List<Task>();
            for (int i = 0; i < 5000; i++)
            {
                tasks.Add(Task.Run(() =>
                {
                    cache.StringSet("test", i);

                    Console.WriteLine(cache.StringGet<string>("test"));
                }));


            }

            Task.WaitAll(tasks.ToArray());

            stopwatch.Stop();

        }
    }
}
