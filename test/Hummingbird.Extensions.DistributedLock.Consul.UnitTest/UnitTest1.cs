using System;
using Consul;
using Hummingbird.Extensions.DistributedLock.Consul;
using Xunit;

namespace Tests
{
    public class Tests
    {
        

        [Fact]
        public void Test1()
        {
            var _client = new ConsulClient(delegate (ConsulClientConfiguration obj)
            {
                obj.Address = new Uri("http://localhost:8500");
                obj.Datacenter = "dc1";
                obj.Token = "";
            });

            ConsulDistributedLock consulDistributedLock = new ConsulDistributedLock(_client, "test");
            consulDistributedLock.Enter("test-lock1", "", TimeSpan.FromSeconds(1));
            consulDistributedLock.Enter("test-lock1", "", TimeSpan.FromSeconds(1));
            consulDistributedLock.Exit("test-lock1","");
        }
    }
}