using System;
using System.Linq;
using Consul;
using Hummingbird.DynamicRoute;
using Xunit;
namespace Hummingbird.Extensions.DynamicRoute.Consul.UnitTest
{
    public class ConsulServiceLocatorUnitTest
    {
        [Fact]
        public async void when_tag_exists_success()
        { 
            var consulClient = new ConsulClient(delegate (ConsulClientConfiguration obj)
            {
                obj.Address = new Uri("http://localhost:8500");
                obj.Datacenter = "dc1";
                obj.Token = "";
            });
            
            IServiceLocator serviceLocator = new ConsulServiceLocator(consulClient);
            var t = await serviceLocator.GetAsync("example", "dev");
            Assert.True(t.Count() > 0);
        }

        [Fact]
        public async void when_tag_notexists_success()
        {
            
            var consulClient = new ConsulClient(delegate (ConsulClientConfiguration obj)
            {
                obj.Address = new Uri("http://localhost:8500");
                obj.Datacenter = "dc1";
                obj.Token = "";
            });
            
            IServiceLocator serviceLocator = new ConsulServiceLocator(consulClient);
            var t = await serviceLocator.GetAsync("example", "ddd");
            Assert.True(t.Count() == 0);
        }

        [Fact]
        public async void when_cross_dc_success()
        {
            var consulClient = new ConsulClient(delegate (ConsulClientConfiguration obj)
            {
                obj.Address = new Uri("http://localhost:8500");
                obj.Datacenter = "dc1";
                obj.Token = "";
            });
            
            IServiceLocator serviceLocator = new ConsulServiceLocator(consulClient);
            var t = await serviceLocator.GetAsync("example", "");
            Assert.True(t.Count() == 0);
        }
    }
}
