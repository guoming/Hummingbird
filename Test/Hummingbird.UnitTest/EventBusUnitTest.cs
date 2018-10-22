using Hummingbird.Extersions.EventBus.SqlServerLogging;
using System.Threading;
using Xunit;
namespace Hummingbird.UnitTest
{
    public class EventBusUnitTest
    {
        Hummingbird.Extersions.EventBus.Abstractions.IEventLogger eventLogger;
        public EventBusUnitTest()
        {
            eventLogger = new SqlServerEventLogger(new DbConnectionFactory("Server=10.2.29.234;Database=HealthCloud.PharmacyService;User Id=sa;Password=kmdb@2016"));

        }

        [Fact]
        public void GetUnPublishedEventList()
        {

            var list = eventLogger.GetUnPublishedEventList(10);
        }


        [Fact]
        public void MarkEventAsPublishedAsyncTest()
        {
            var list = eventLogger.MarkEventAsPublishedAsync(new System.Collections.Generic.List<string>() { "fbdd9768-f79b-4fc5-a69f-37fc4ea3a332" }, CancellationToken.None);
        }


        [Fact]
        public void MarkEventAsPublishedFailedAsync()
        {
            var list = eventLogger.MarkEventAsPublishedFailedAsync(new System.Collections.Generic.List<string>() { "fbdd9768-f79b-4fc5-a69f-37fc4ea3a332" }, CancellationToken.None);
        }


        [Fact]
        public void MarkEventConsumeAsFailedAsync()
        {
            eventLogger.MarkEventConsumeAsFailedAsync(new[] { "fbdd9768-f79b-4fc5-a69f-37fc4ea3a332" }, "Test",CancellationToken.None).Wait();
        }

        [Fact]
        public void MarkEventConsumeAsRecivedAsync()
        {
            eventLogger.MarkEventConsumeAsRecivedAsync(new[] { "fbdd9768-f79b-4fc5-a69f-37fc4ea3a332" }, "Test", CancellationToken.None).Wait();
        }

        [Fact]
        public void SaveEventAsyncTest()
        {
            System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
            stopwatch.Start();
            var _dbConnection = new DbConnectionFactory("Server=10.2.29.234;Database=HealthCloud.PharmacyService;User Id=sa;Password=kmdb@2016");

            SqlServerEventLogger eventLogService = new SqlServerEventLogger(_dbConnection);

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
                    eventLogService.SaveEventAsync(eventLis, transaction).Wait();
                    transaction.Commit();
                }
            }

            stopwatch.Stop();
            
            
        }
    }
}
