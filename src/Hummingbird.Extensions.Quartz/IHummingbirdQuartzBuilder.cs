using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace Hummingbird.Extensions.Tracing
{
    public interface IHummingbirdQuartzBuilder
    {
         IServiceCollection Services { get; }
    }
}
