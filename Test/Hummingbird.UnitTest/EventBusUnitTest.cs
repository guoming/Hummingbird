using Hummingbird.Extersions.EventBus.SqlServerLogging;
using System.Threading;
using Xunit;
using System.Linq;
namespace Hummingbird.UnitTest
{
    public class EventBusUnitTest
    {
        private string ConnectionString = "Data Source=192.168.109.227,63341;Initial Catalog=zongteng_TMS-dev;User ID=zt-2874-dev;pwd=qXSW!9vXYfFxQYbg";
        Hummingbird.Extersions.EventBus.Abstractions.IEventLogger eventLogger;
        Hummingbird.Extersions.EventBus.Abstractions.IEventBus eventBus;

        public EventBusUnitTest()
        {
            var cacheManager = Hummingbird.Extersions.Cache.CacheFactory.Build<bool>(config => { });
            var uniqueIdGenerator = new Hummingbird.Extersions.UidGenerator.SnowflakeUniqueIdGenerator(1, 1);
            var SqlConfig = new SqlServerConfiguration();
            SqlConfig.WithEndpoint(ConnectionString);

            eventLogger = new SqlServerEventLogger(uniqueIdGenerator,new DbConnectionFactory(ConnectionString), SqlConfig);
        }

        [Fact]
        public void GetUnPublishedEventList()
        {

            var list = eventLogger.GetUnPublishedEventList(10);
        }


        [Fact]
        public void MarkEventAsPublishedAsyncTest()
        {
            var list = eventLogger.MarkEventAsPublishedAsync(new System.Collections.Generic.List<long>() {  0L }, CancellationToken.None);
        }


        [Fact]
        public void MarkEventAsPublishedFailedAsync()
        {
            var list = eventLogger.MarkEventAsPublishedFailedAsync(new System.Collections.Generic.List<long>() { 0L }, CancellationToken.None);
        }


        [Fact]
        public void SaveEventAsyncTest()
        {
            System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
            stopwatch.Start();
            var _dbConnection = new DbConnectionFactory("Server=10.2.29.234;Database=HealthCloud.PharmacyService;User Id=sa;Password=kmdb@2016");
            var SqlConfig = new SqlServerConfiguration();
            SqlConfig.WithEndpoint(_dbConnection.ConnectionString);

            var eventList = new System.Collections.Generic.List<object> {
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

                    eventLogger.SaveEventAsync(eventList.Select(@event=>new Hummingbird.Extersions.EventBus.Models.EventLogEntry("",@event)).ToList(), transaction).Wait();
                    transaction.Commit();
                }
            }

            stopwatch.Stop();
            
            
        }
    }
}
