using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace Hummingbird.Extersions.EventBus
{
    public interface IHummingbirdEventBusHostBuilder
    {
        IServiceCollection Services { get; }
    }
}

