using System;
using System.Collections.Generic;

namespace Hummingbird.LoadBalancers
{
    public class DefaultLoadBalancerFactory<T> : ILoadBalancerFactory<T>
    {

        public ILoadBalancer<T> Get(Func<List<T>> func,string Type= "RoundRobin")
        {
#pragma warning disable S3923 // All branches in a conditional structure should not have exactly the same implementation
            switch (Type)
            {
                case "RoundRobin":                    
                case "RoundRobinLoadBalancer":
                    return new RoundRobinLoadBalancer<T>(func);
                case "RandomRobin":
                case "RandomRobinLoadBalancer":
                    return new RandomRobinLoadBalancer<T>(func);
                default:
                    return new RoundRobinLoadBalancer<T>(func);
            }
#pragma warning restore S3923 // All branches in a conditional structure should not have exactly the same implementation
        }
    }
}
