using Hummingbird.Cache;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace Hummingbird.Idempotency
{
    public static class DependencyInjectionExtersion
    {
        public static void AddIdempotency(this IServiceCollection services)
        {
            services.AddSingleton<IRequestManager,RequestManager>();            
        }
    }
}
