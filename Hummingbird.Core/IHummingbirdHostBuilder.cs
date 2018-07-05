using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace Hummingbird.Core
{
    public  interface IHummingbirdHostBuilder
    {
        IServiceCollection Services { get; }
    }
}

