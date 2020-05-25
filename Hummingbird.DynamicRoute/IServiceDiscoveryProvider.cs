using System;
using System.Collections.Generic;
using System.Text;

namespace Hummingbird.DynamicRoute
{
    public interface IServiceDiscoveryProvider
    {
        void Register();

        void Deregister();

        string ServiceId { get;  }
    }
}
