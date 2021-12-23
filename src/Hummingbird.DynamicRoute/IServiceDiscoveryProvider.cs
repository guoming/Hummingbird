using System;
using System.Collections.Generic;
using System.Text;

namespace Hummingbird.DynamicRoute
{
    public interface IServiceDiscoveryProvider
    {
        void Register();

        void Deregister();

        void Heartbeat();

        string ServiceId { get;  }
    }
}
