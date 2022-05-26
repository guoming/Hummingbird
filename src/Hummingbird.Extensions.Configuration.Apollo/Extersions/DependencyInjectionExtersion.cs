using System;
using System.Collections.Generic;
using Com.Ctrip.Framework.Apollo;
using Com.Ctrip.Framework.Apollo.Enums;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class DependencyInjectionExtersion
    {
        
        public static IConfigurationBuilder AddApolloConfiguration(this IConfigurationBuilder builder, IConfiguration configuration,Dictionary<string,ConfigFileFormat> namespaces)
        {
          IApolloConfigurationBuilder apolloConfigurationBuilder=  builder.AddApollo(configuration)
              .AddDefault();
          
            foreach (var configFileFormat in namespaces)
            {
                apolloConfigurationBuilder.AddNamespace(configFileFormat.Key, configFileFormat.Value);
            }
            
            builder.Build();

            return builder;

        }
    }
}