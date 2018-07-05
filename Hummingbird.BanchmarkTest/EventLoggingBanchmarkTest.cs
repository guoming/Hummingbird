using Hummingbird.Extersions.EventBus.SqlServerLogging;
using System;
using System.Collections.Generic;
using System.Text;

namespace Hummingbird.BanchmarkTest
{
    /// <summary>
    /// 事件日志持久化基准测试
    /// </summary>
    class EventLoggingBanchmarkTest
    {
       public void Run()
        {
            Action<int> action = (i) => {
                System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
                stopwatch.Start();
                var _dbConnection = new DbConnectionFactory("Min Pool Size=10;Max Pool Size=50;Server=10.2.29.234;Database=HealthCloud.PharmacyService;User Id=sa;Password=kmdb@2016");
                var eventLogger = new SqlServerEventLogger(_dbConnection);

                var eventLis = new System.Collections.Generic.List<object> {
                new { EventId = "1" },
                new { EventId = "2" } };

                using (var db = _dbConnection.GetDbConnection())
                {
                    if (db.State != System.Data.ConnectionState.Open)
                    {
                        db.Open();
                    }

                    using (var transaction = db.BeginTransaction())
                    {
                        eventLogger.SaveEventAsync(eventLis, transaction).Wait();
                        transaction.Commit();
                    }
                }
                stopwatch.Stop();

                Console.WriteLine($"{i}:{System.Threading.Thread.CurrentThread.Name} 持久化{eventLis.Count}条消息 耗时{stopwatch.ElapsedMilliseconds}毫秒");

            };

            System.Diagnostics.Stopwatch stopwatchAll = new System.Diagnostics.Stopwatch();
            System.Threading.Tasks.Dataflow.ActionBlock<int> actionBlock1 = new System.Threading.Tasks.Dataflow.ActionBlock<int>(action, new System.Threading.Tasks.Dataflow.ExecutionDataflowBlockOptions()
            {

                MaxDegreeOfParallelism = 8, // 并行4个

            });
            stopwatchAll.Start();
            for (var i = 0; i < 100; i++)
            {
                actionBlock1.Post(i);
            }


            actionBlock1.Complete();
            actionBlock1.Completion.Wait();
            stopwatchAll.Stop();
            Console.WriteLine($"第一轮，总耗时{stopwatchAll.ElapsedMilliseconds}毫秒");

            System.Threading.Tasks.Dataflow.ActionBlock<int> actionBlock2 = new System.Threading.Tasks.Dataflow.ActionBlock<int>(action, new System.Threading.Tasks.Dataflow.ExecutionDataflowBlockOptions()
            {

                MaxDegreeOfParallelism = 8, // 并行4个

            });
            stopwatchAll.Restart();
            for (var i = 0; i < 100; i++)
            {
                actionBlock2.Post(i);
            }

            actionBlock2.Complete();
            actionBlock2.Completion.Wait();
            stopwatchAll.Stop();
            Console.WriteLine($"第二轮，总耗时{stopwatchAll.ElapsedMilliseconds}毫秒");
        }
    }
}
