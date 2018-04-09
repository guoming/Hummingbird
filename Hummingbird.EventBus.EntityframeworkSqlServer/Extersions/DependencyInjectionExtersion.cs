using Autofac;
using Hummingbird.EventBus.Abstractions;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Polly;
using Polly.Retry;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Reflection;

namespace Hummingbird.EventBus.EntityframeworkSqlServer.Extersions
{
    public static class DependencyInjectionExtersion
    {
        public static IServiceCollection AddEventBusSqlServer(this IServiceCollection services,string ConnectionString)
        {
            services.AddDbContext<EventLogContext>(options =>
            {
                options.UseSqlServer(ConnectionString,
                                     sqlServerOptionsAction: sqlOptions =>
                                     {
                                         sqlOptions.MigrationsAssembly(typeof(DependencyInjectionExtersion).GetTypeInfo().Assembly.GetName().Name);
                                         sqlOptions.EnableRetryOnFailure(maxRetryCount: 10, maxRetryDelay: TimeSpan.FromSeconds(30), errorNumbersToAdd: null);
                                         
                                     })
                        .ConfigureWarnings(warnings => warnings.Throw(RelationalEventId.QueryClientEvaluationWarning));

            }, ServiceLifetime.Transient);
   

            services.AddTransient<IEventLogService, EventLogService>();

            return services;
        }
    }
}
