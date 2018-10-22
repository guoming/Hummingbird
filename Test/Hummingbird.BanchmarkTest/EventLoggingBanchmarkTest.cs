using BenchmarkDotNet.Attributes;
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

       [Benchmark]
       public void SaveEventAsync()
        {
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
        }
    }
}
