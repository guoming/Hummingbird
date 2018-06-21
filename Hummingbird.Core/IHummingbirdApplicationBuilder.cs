using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace Hummingbird.Core
{
    public interface IHummingbirdApplicationBuilder
    {
        IApplicationBuilder app { get; }
    }
}

