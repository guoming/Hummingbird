using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace Hummingbird.Extensions.Cacheing.UnitTest
{
    public class UnitTest1
    {
        [Fact]
        public void Test1()
        {


            var cache= Hummingbird.Extensions.Cacheing.CacheFactory.Build(option=> {

                option.WithReadServerList("127.0.0.1:6378");
                option.WithWriteServerList("127.0.0.1:6378");
                option.WithDb(0);

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


        [Fact]
        public void StringSet()
        {


            var cache = Hummingbird.Extensions.Cacheing.CacheFactory.Build(option => {

                option.WithReadServerList("127.0.0.1:6379");
                option.WithWriteServerList("127.0.0.1:6379");
                option.WithDb(0);

            });
            cache.StringSet("test", "bb");

            System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
            stopwatch.Start();
            cache.StringSet("test", "aa");


            stopwatch.Stop();


        }
    }
}
