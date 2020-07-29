using App.Metrics;
using App.Metrics.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using System;
using System.Linq;

namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class DependencyInjectionExtersion
    {
        public static IServiceCollection AddMetrics(this IServiceCollection services, IConfigurationSection configurationSection)
        {
            var metricsBuilder = App.Metrics.AppMetrics.CreateDefaultBuilder()
                .OutputMetrics.AsPrometheusPlainText()
                .OutputMetrics.AsPrometheusProtobuf()
                .Configuration.Configure(options =>
                {
                    options.AddEnvTag();
                    options.AddAppTag();
                    options.AddServerTag();
                    options.Enabled = true;
                })
                .ToInfluxDb(configurationSection);

            services.AddSingleton<App.Metrics.IMetricsRoot>(metricsBuilder.Build());
            return services;
        }

        
        public static IWebHostBuilder UseMetrics(this IWebHostBuilder hostBuilder, Action<WebHostBuilderContext, IMetricsBuilder> configureMetrics)
        {
            hostBuilder.ConfigureMetrics(configureMetrics);

            return hostBuilder.UseMetrics(options =>
            {
                options.EndpointOptions = endpointsOptions =>
                {
                    endpointsOptions.EnvironmentInfoEndpointEnabled = true;
                    endpointsOptions.MetricsTextEndpointEnabled = false;
                    endpointsOptions.MetricsEndpointEnabled = true;
                    endpointsOptions.MetricsEndpointOutputFormatter = Metrics.Instance.OutputMetricsFormatters.OfType<App.Metrics.Formatters.Prometheus.MetricsPrometheusTextOutputFormatter>().First();
                };
            });
        }

        public static IMetricsBuilder ToPrometheus(this IMetricsBuilder metricsBuilder)
        {
            metricsBuilder.OutputMetrics.AsPrometheusPlainText();
            metricsBuilder.OutputMetrics.AsPrometheusProtobuf();
            return metricsBuilder;
        }

        #region Metrics

        public static IMetricsBuilder ToInfluxDb(this IMetricsBuilder metricsBuilder, IConfigurationSection configurationSection)
        {
            #region report to influxdb
            if (configurationSection.Exists())
            {
                var appMetrics_Influxdb_Enable = configurationSection["Enable"];
                if (appMetrics_Influxdb_Enable.ToLower() == bool.TrueString.ToLower())
                {
                    metricsBuilder.Report.ToInfluxDb(options =>
                    {

                        var appMetrics_influxdb_address = configurationSection["Address"];
                        var appMetrics_influxdb_database = configurationSection["Database"];
                        var appMetrics_influxdb_username = configurationSection["UserName"];
                        var appMetrics_influxdb_password = configurationSection["Password"];

                        options.InfluxDb = new App.Metrics.Reporting.InfluxDB.InfluxDbOptions()
                        {
                            BaseUri = new Uri(appMetrics_influxdb_address),
                            Database = appMetrics_influxdb_database,
                            UserName = appMetrics_influxdb_username,
                            Password = appMetrics_influxdb_password,
                            CreateDataBaseIfNotExists = true,
                        };
                        options.HttpPolicy = new App.Metrics.Reporting.InfluxDB.Client.HttpPolicy
                        {
                            FailuresBeforeBackoff = int.Parse(configurationSection["Options:FailuresBeforeBackoff"] ?? "3"),
                            BackoffPeriod = TimeSpan.FromSeconds(int.Parse(configurationSection["Options:BackoffPeriod"] ?? "30")),
                            Timeout = TimeSpan.FromSeconds(int.Parse(configurationSection["Options:BackoffPeriod"] ?? "15"))
                        };
                        options.FlushInterval = TimeSpan.FromSeconds(int.Parse(configurationSection["Options:FlushInterval"] ?? "5"));

                    });
                }
            }
            #endregion

            return metricsBuilder;
        }
        #endregion
    }
}
