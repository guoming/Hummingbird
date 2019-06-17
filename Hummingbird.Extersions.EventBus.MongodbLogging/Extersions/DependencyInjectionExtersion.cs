using Microsoft.Extensions.DependencyInjection;
using Hummingbird.Core;
using Hummingbird.Extersions.EventBus;
using Hummingbird.Extersions.EventBus.Abstractions;
using System;
using Hummingbird.Extersions.EventBus.MongodbLogging;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.IdGenerators;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class DependencyInjectionExtersion
    {
        public static IHummingbirdEventBusHostBuilder AddMongodbEventLogging(this IHummingbirdEventBusHostBuilder hostBuilder, MongodbConfiguration configuration)
        {
            var classMap = new BsonClassMap(typeof(Hummingbird.Extersions.EventBus.Models.EventLogEntry)).MapIdField("MessageId").SetIdGenerator(StringObjectIdGenerator.Instance).ClassMap;
            BsonClassMap.RegisterClassMap(classMap);
        
            hostBuilder.Services.AddTransient<MongodbConfiguration>(a => configuration);
            hostBuilder.Services.AddTransient<MongoDB.Driver.IMongoClient>((sp) => {
                var config = sp.GetService<MongodbConfiguration>();
                var mongoUrl = MongoDB.Driver.MongoUrl.Create(config.ConnectionString);
                var client = new MongoDB.Driver.MongoClient(mongoUrl);

                return client;
            });
            hostBuilder.Services.AddTransient<IEventLogger, MongodbEventLogger>();
            return hostBuilder;
        }
    }
}
