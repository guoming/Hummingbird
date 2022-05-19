using System.Linq;
using Hummingbird.DynamicRoute;
using Xunit;
namespace Hummingbird.Extensions.DynamicRoute.Consul.UnitTest
{
    public class ConsulServiceLocatorTest
    {
        [Fact]
        public async void when_tag_exists_success()
        {
            IServiceLocator serviceLocator = new ConsulServiceLocator("localhost", "8500", "dc1", "");
            var t = await serviceLocator.GetAsync("example", "dev");
            Assert.True(t.Count() > 0);
        }

        [Fact]
        public async void when_tag_notexists_success()
        {
            IServiceLocator serviceLocator = new ConsulServiceLocator("localhost", "8500", "dc1", "");
            var t = await serviceLocator.GetAsync("example", "ddd");
            Assert.True(t.Count() == 0);
        }

        [Fact]
        public async void when_cross_dc_success()
        {
            IServiceLocator serviceLocator = new ConsulServiceLocator("localhost", "8500", "dc2", "");
            var t = await serviceLocator.GetAsync("example", "");
            Assert.True(t.Count() == 0);
        }
    }
}
