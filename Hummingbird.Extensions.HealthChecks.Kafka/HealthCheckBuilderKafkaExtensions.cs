// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Confluent.Kafka;
using System;
using System.Data;

namespace Hummingbird.Extensions.HealthChecks
{

    public class KafkaOption
    {

        internal Confluent.Kafka.ProducerConfig config { get; set; } = new Confluent.Kafka.ProducerConfig() { BootstrapServers = "localhost:9092" };

        public void WithConfig(Confluent.Kafka.ProducerConfig config)
        {
            this.config = config;
        }
    }

    public static class HealthCheckBuilderKafkaExtensions
    {
        public static HealthCheckBuilder AddKafkaCheck(this HealthCheckBuilder builder, string name, Action<KafkaOption> setup)
        {
            Guard.ArgumentNotNull(nameof(builder), builder);

            return AddKafkaCheck(builder, name, setup, builder.DefaultCacheDuration);
        }

        public static HealthCheckBuilder AddKafkaCheck(this HealthCheckBuilder builder, string name, Action<KafkaOption> setup, TimeSpan cacheDuration)
        {
            var option = new KafkaOption();
            setup(option);

            var producerBuilder = new ProducerBuilder<string, string>(option.config);
            var producer = producerBuilder.Build();

            builder.AddCheck($"KafkaCheck({name})", () =>
            {
                try
                {
                    
                 
                    var i = producer.Flush(TimeSpan.FromMilliseconds(50));
                    
                    if (i >= 0)
                    {
                        return HealthCheckResult.Healthy($"Healthy");
                    }
                    else
                    {
                        return HealthCheckResult.Unhealthy($"Unhealthy");

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
