using System;
using Consul;
using Hummingbird.Extensions.DistributedLock.Consul;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Hummingbird.Extensions.DistributedLock.Consul.UnitTest
{
    public class ConsulDistributedLockUnitTest
    {
        

        [Fact]
        public void Test1()
        {
            var client = new ConsulClient(delegate (ConsulClientConfiguration obj)
            {
                obj.Address = new Uri("http://localhost:8500");
                obj.Datacenter = "dc1";
                obj.Token = "";
            });

            ConsulDistributedLock consulDistributedLock = new ConsulDistributedLock(client, null,"test");
            consulDistributedLock.Enter("test-lock1", "");
            consulDistributedLock.Enter("test-lock1", "");
            consulDistributedLock.Exit("test-lock1","");
        }
    }
}