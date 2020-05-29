using System;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Hummingbird.Extensions.HealthChecks
{
    using System.Linq;
    using Hummingbird.Extensions.HealthChecks;
    using MongoDB.Driver.Core.Servers;

    public static class HealthCheckBuilderMongoDbExtensions
    {
        public static HealthCheckBuilder AddMongoDbCheck(this HealthCheckBuilder builder, string name, string connectionString, string databaseName)
        {
            Guard.ArgumentNotNull(nameof(builder), builder);

            return AddMongoDbCheck(builder, name, connectionString, databaseName, builder.DefaultCacheDuration);
        }

        public static HealthCheckBuilder AddMongoDbCheck(this HealthCheckBuilder builder, string name, string connectionString, string databaseName, TimeSpan cacheDuration)
        {
            builder.AddCheck($"MongoDbCheck({name})", async () =>
            {
                try
                {
                    var client = new MongoClient(connectionString);
                    IMongoDatabase database = client.GetDatabase(databaseName);

                    BsonDocument result = await database.RunCommandAsync((Command<BsonDocument>)"{ping:1}").ConfigureAwait(false);

                    ServerState serverState = client.Cluster.Description.Servers.FirstOrDefault()?.State
                                          ?? ServerState.Disconnected;
                    if (serverState == ServerState.Disconnected)
                    {
                        return HealthCheckResult.Unhealthy($"Unhealthy");
                    }
                    else
                    {

                        return HealthCheckResult.Healthy($"Healthy");
                    }
                }
                catch (Exception ex)
                {
                    return HealthCheckResult.Unhealthy($"{ex.GetType().FullName}");
                }
            }, cacheDuration);

            return builder;
        }
    }
}