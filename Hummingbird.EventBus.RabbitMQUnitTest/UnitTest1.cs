using Hummingbird.Extersions.Dapper.SqlServer;
using Hummingbird.Extersions.EventBus.SqlServer;
using Microsoft.Extensions.DependencyInjection;
using System;
using Xunit;
namespace Hummingbird.EventBus.RabbitMQUnitTest
{
    public class UnitTest1
    {
     
        [Fact]
        public void GetUnPublishedEventList()
        {
            SqlServerEventLogger eventLogService = new SqlServerEventLogger(new DbConnectionFactory("Server=10.2.29.234;Database=HealthCloud.PharmacyService;User Id=sa;Password=kmdb@2016"));

            var list = eventLogService.GetUnPublishedEventList(10);
        }


        [Fact]
        public void MarkEventAsPublishedAsyncTest()
        {
            SqlServerEventLogger eventLogService = new SqlServerEventLogger(new DbConnectionFactory("Server=10.2.29.234;Database=HealthCloud.PharmacyService;User Id=sa;Password=kmdb@2016"));
            var list = eventLogService.MarkEventAsPublishedAsync(new System.Collections.Generic.List<string>() { "fbdd9768-f79b-4fc5-a69f-37fc4ea3a332" });


        }


        [Fact]
        public void MarkEventAsPublishedFailedAsync()
        {
            SqlServerEventLogger eventLogService = new SqlServerEventLogger(new DbConnectionFactory("Server=10.2.29.234;Database=HealthCloud.PharmacyService;User Id=sa;Password=kmdb@2016"));
            var list = eventLogService.MarkEventAsPublishedFailedAsync(new System.Collections.Generic.List<string>() { "fbdd9768-f79b-4fc5-a69f-37fc4ea3a332" });
        }


        [Fact]
        public void MarkEventConsumeAsFailedAsync()
        {
            SqlServerEventLogger eventLogService = new SqlServerEventLogger(new DbConnectionFactory("Server=10.2.29.234;Database=HealthCloud.PharmacyService;User Id=sa;Password=kmdb@2016"));
            eventLogService.MarkEventConsumeAsFailedAsync( "fbdd9768-f79b-4fc5-a69f-37fc4ea3a332" ,"Test").Wait();
        }

        [Fact]
        public void MarkEventConsumeAsRecivedAsync()
        {
            SqlServerEventLogger eventLogService = new SqlServerEventLogger(new DbConnectionFactory("Server=10.2.29.234;Database=HealthCloud.PharmacyService;User Id=sa;Password=kmdb@2016"));
            eventLogService.MarkEventConsumeAsRecivedAsync("fbdd9768-f79b-4fc5-a69f-37fc4ea3a332","Test").Wait();
        }

        [Fact]
        public void SaveEventAsyncTest()
        {
            var _dbConnection = new DbConnectionFactory("Server=10.2.29.234;Database=HealthCloud.PharmacyService;User Id=sa;Password=kmdb@2016");

            SqlServerEventLogger eventLogService = new SqlServerEventLogger(_dbConnection);

            using (var db = _dbConnection.GetDbConnection())
            {
                if (db.State != System.Data.ConnectionState.Open)
                {
                    db.Open();
                }

                using (var transaction = db.BeginTransaction()) {
                    eventLogService.SaveEventAsync(new System.Collections.Generic.List<object> { new { EventId = "1" }, new { EventId = "2" } },transaction).Wait();

                    transaction.Commit();
                }
            }
        }
    }
}
 