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
        public static IHummingbirdEventBusHostBuilder AddMongodbEventLogging(this IHummingbirdEventBusHostBuilder hostBuilder, Action<MongodbConfiguration> setupConnectionFactory)
        {
            #region 配置
            setupConnectionFactory = setupConnectionFactory ?? throw new ArgumentNullException(nameof(setupConnectionFactory));
            var configuration = new MongodbConfiguration();
            setupConnectionFactory(configuration);
            #endregion

            #region Mongodb 主键映射
            BsonClassMap.RegisterClassMap<Hummingbird.Extersions.EventBus.Models.EventLogEntry>(map =>
            {
                map.AutoMap();
                map.SetIgnoreExtraElements(true);//忽略属性
                map.MapProperty("MessageId").SetIdGenerator(StringObjectIdGenerator.Instance);
                map.MapProperty(c => c.Content).SetElementName("Content");
                map.MapProperty(c => c.CreationTime).SetElementName("CreationTime");
                map.MapProperty(c => c.EventId).SetElementName("EventId");
                map.MapProperty(c => c.EventTypeName).SetElementName("EventTypeName");
                map.MapProperty(c => c.State).SetElementName("State");
                map.MapProperty(c => c.TimesSent).SetElementName("TimesSent");                
            });
       
            #endregion

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
