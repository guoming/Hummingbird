using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace Hummingbird.Extensions.UidGenerator.Abastracts
{
    public interface IWorkIdCreateStrategyBuilder
    {
        IServiceCollection Services { get; }

        int CenterId { get; set; }
    }
}
