using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace Hummingbird.Extensions.Tracing
{
    public interface IHummingbirdOpenTracingBuilder
    {
         IServiceCollection Services { get; }
    }
}
