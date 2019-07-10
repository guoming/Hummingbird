using Hummingbird.Extersions.Cacheing.StackExchangeImplement;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Hummingbird.Extersions.Cacheing.LoadBalancers
{
    internal interface IConnectionLoadBalancer
    {
        RedisClientHelper Lease();

    }
}
