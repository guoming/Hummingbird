using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace Hummingbird.Extensions.RequestLimit
{
    public interface IHummingbirdRequestLimitBuilder
    {
        IServiceCollection Services { get; }
    }
}

