using System;
using Hummingbird.Extensions.OpenTracing.Jaeger;
using Hummingbird.Extensions.Tracing;
using Microsoft.Extensions.Configuration;
using OpenTracing;

namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class DependencyInjectionExtersion
    {
        #region OpenTracking
        public static IHummingbirdOpenTracingBuilder AddJaeger(this IHummingbirdOpenTracingBuilder builder, IConfigurationSection configurationSection, Action<IOpenTracingBuilder> openTracingBuilder=null)
        {
            builder.Services.AddTransient<TracingConfiguration>(sp =>
            {
                var config = configurationSection.Get<TracingConfiguration>();
                if (config == null)
                {
                    config = new TracingConfiguration() { Open = false };
                }
                 

                return config;
            });
            AddJaeger(builder.Services, openTracingBuilder);

            return builder;
        }

        public static IHummingbirdOpenTracingBuilder AddJaeger(this IHummingbirdOpenTracingBuilder builder, Action<TracingConfiguration> action,Action<IOpenTracingBuilder> openTracingBuilder=null)
        {
            var config = new TracingConfiguration() { Open = false };
            action = action ?? throw new ArgumentNullException(nameof(action));
            action(config);

            builder.Services.AddTransient<TracingConfiguration>(sp =>
            {
                return config;
            });
            AddJaeger(builder.Services, openTracingBuilder);

            return builder;
        }

     

        static IServiceCollection AddJaeger(this IServiceCollection services, Action<IOpenTracingBuilder> openTracingBuilder=null)
        {

            #region OpenTracing

            if(openTracingBuilder==null)
            {
                openTracingBuilder = builder =>
                {
                    builder.AddCoreFx();
                    builder.AddAspNetCore();
                    builder.AddEntityFrameworkCore();
                    builder.AddLoggerProvider();                    
                    builder.ConfigureGenericDiagnostics(options =>
                    {
                        
                    });
                    builder.ConfigureAspNetCore(options =>
                    {

                        options.Hosting.OperationNameResolver = (context) =>
                        {

                            return context.Request.Path.ToUriComponent();
                        };
                        options.Hosting.IgnorePatterns.Add(a =>
                        {
                            return false;
                        });

                    });
                };
            }

            services.AddOpenTracing(openTracingBuilder);
            services.AddSingleton<ITracer>(serviceProvider =>
            {
                var config = serviceProvider.GetService<TracingConfiguration>();
                var serviceName = config.SerivceName;
                var loggerFactory = serviceProvider.GetRequiredService<Microsoft.Extensions.Logging.ILoggerFactory>();
                var endPoint = config.EndPoint;
                var senderConfiguration = new Jaeger.Configuration.SenderConfiguration(loggerFactory);

                if (!string.IsNullOrEmpty(config.AgentHost))
                {
                    senderConfiguration
                    .WithAgentHost(config.AgentHost)
                    .WithAgentPort(config.AgentPort);
                }
                else
                {
                    senderConfiguration.WithEndpoint(endPoint);
                }

                var samplerConfiguration = new Jaeger.Configuration.SamplerConfiguration(loggerFactory)
                    .WithType(config.SamplerType);
                var reporterConfiguration = new Jaeger.Configuration.ReporterConfiguration(loggerFactory)
                    .WithFlushInterval(TimeSpan.FromSeconds(config.FlushIntervalSeconds))
                    .WithLogSpans(config.LogSpans)
                    .WithSender(senderConfiguration);

                ITracer tracer = null;
                if (config.Open)
                {
                    tracer = new Jaeger.Configuration(serviceName, loggerFactory)
                       .WithSampler(samplerConfiguration)
                       .WithReporter(reporterConfiguration)
                       .GetTracer();
                }
                else
                {
                    tracer = new Jaeger.Tracer.Builder(serviceName)
                    .WithSampler(new Jaeger.Samplers.RateLimitingSampler(0))
                     .WithReporter(new Jaeger.Reporters.NoopReporter()).Build();
                }

                if (!OpenTracing.Util.GlobalTracer.IsRegistered())
                {
                    OpenTracing.Util.GlobalTracer.Register(tracer);
                }


                return tracer;
            });
            #endregion

            return services;
        }
        #endregion



    }

 
}
