using CanalSharp.Client;
using CanalSharp.Client.Impl;
using Hummingbird.Core;
using Hummingbird.Extensions.Canal;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class DependencyInjectionExtersion
    {
        public static IHummingbirdHostBuilder AddCanal(this IHummingbirdHostBuilder hostBuilder, IConfigurationSection configurationSection)
        {
            hostBuilder.Services.AddSingleton<CanalConfig>(sp =>
            {
                return configurationSection.Get<CanalConfig>();

            });
            hostBuilder.Services.AddHostedService<CanalClientHostedService>();
            return hostBuilder;

          
        }
    }
}
