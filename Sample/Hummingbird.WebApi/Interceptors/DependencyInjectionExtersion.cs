using Autofac;
using Autofac.Extras.DynamicProxy;
using Hummingbird.NetCoreWebApi.Interceptors;
using System;
using System.Linq;

namespace Hummingbird.NetCoreWebApi
{
    public static partial class DependencyInjectionExtersion
    {
        public static void AddInterceptors(this ContainerBuilder builder)
        {
            #region AOP

            builder.RegisterType<TracerInterceptor>();
            builder.RegisterType<MetricInterceptor>();

            var types = AppDomain.CurrentDomain.GetAssemblies()
                      .SelectMany(a => a.GetTypes().Where(type => Array.Exists(type.GetInterfaces(), t => 
                      t.IsGenericType                     
                   
                      && (t.GetGenericTypeDefinition() == typeof(Hummingbird.Extersions.EventBus.Abstractions.IEventHandler<>)
                      || t.GetGenericTypeDefinition() == typeof(Hummingbird.Extersions.EventBus.Abstractions.IEventBatchHandler<>)
                      ))))
                      .ToArray();

            foreach (var type in types)
            {
                builder.RegisterType(type)
                  .InstancePerLifetimeScope()
                  .EnableClassInterceptors().InterceptedBy(typeof(TracerInterceptor), typeof(MetricInterceptor));
            }
            #endregion

        }
    }
}
