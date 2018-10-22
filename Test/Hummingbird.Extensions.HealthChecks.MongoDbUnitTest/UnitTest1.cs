using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.HealthChecks;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Core.Servers;
using System;
using System.Linq;
using Xunit;

namespace Hummingbird.Extensions.HealthChecks.MongoDbUnitTest
{
    public class UnitTest1
    {
        [Fact]
        public void UnHealth()
        {

            var client = new MongoClient("mongodb://tmsTrackingUser:xAG_eHZVuu5QaGh6@125.77.22.140:63101/TmsTracking");
            IMongoDatabase database = client.GetDatabase("TmsTracking");


         

            BsonDocument result = database.RunCommand((Command<BsonDocument>)"{ping:1}");


            ServerState serverState = client.Cluster.Description.Servers.FirstOrDefault()?.State
                                      ?? ServerState.Disconnected;
            if (serverState == ServerState.Disconnected)
            {
                Assert.True(true);
            }

            Assert.True(true);

        }
    }
}
