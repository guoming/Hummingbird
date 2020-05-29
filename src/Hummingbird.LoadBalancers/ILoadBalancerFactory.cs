using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Hummingbird.LoadBalancers
{
    public interface ILoadBalancerFactory<T>
    {
        ILoadBalancer<T> Get(Func<List<T>> func,string Type);
    }
}