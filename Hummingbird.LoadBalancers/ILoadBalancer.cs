using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Hummingbird.LoadBalancers
{
    public interface ILoadBalancer<T>
    {
        T Lease();

    }
}