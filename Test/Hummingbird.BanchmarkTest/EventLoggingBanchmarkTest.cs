using BenchmarkDotNet.Attributes;
using Hummingbird.Extersions.EventBus.SqlServerLogging;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace Hummingbird.BanchmarkTest
{
    /// <summary>
    /// 事件日志持久化基准测试
    /// </summary>
    public class EventLoggingBanchmarkTest
    {

       [Benchmark]
       public void SaveEventAsync()
        {
            var _dbConnection = new DbConnectionFactory("Data Source=192.168.109.227,63341;Initial Catalog=zongteng_TMS-dev;User ID=zt-2874-dev;pwd=qXSW!9vXYfFxQYbg");
            var uniqueIdGenerator = new Hummingbird.Extersions.UidGenerator.SnowflakeUniqueIdGenerator(1, 1);
            var SqlConfig = new SqlServerConfiguration();
            SqlConfig.WithEndpoint(_dbConnection.ConnectionString);

            var eventLogger = new SqlServerEventLogger(uniqueIdGenerator,_dbConnection, SqlConfig);

            var items = new System.Collections.Generic.List<object> {
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
                    eventLogger.SaveEventAsync(items.Select(@event=>new Hummingbird.Extersions.EventBus.Models.EventLogEntry("",@event)).ToList(), transaction).Wait();
                    transaction.Commit();
                }
            }
        }

        [Benchmark]
        public  void SaveEventAndMarkEventAsPublishedAsync()
        {
            var _dbConnection = new DbConnectionFactory("Data Source=192.168.109.227,63341;Initial Catalog=zongteng_TMS-dev;User ID=zt-2874-dev;pwd=qXSW!9vXYfFxQYbg");
            var uniqueIdGenerator = new Hummingbird.Extersions.UidGenerator.SnowflakeUniqueIdGenerator(1, 1);
            var SqlConfig = new SqlServerConfiguration();
            SqlConfig.WithEndpoint(_dbConnection.ConnectionString);
            var eventLogger = new SqlServerEventLogger(uniqueIdGenerator, _dbConnection, SqlConfig);

            var items = new System.Collections.Generic.List<object> {
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
                    var task = eventLogger.SaveEventAsync(items.Select(@event=>new Hummingbird.Extersions.EventBus.Models.EventLogEntry("",@event)).ToList(), transaction);
                    task.Wait();
                    
                    eventLogger.MarkEventAsPublishedAsync(task.Result.Select(a=>a.EventId).ToList(), System.Threading.CancellationToken.None).Wait();
                    transaction.Commit();
                }
            }
        }
    }
}
