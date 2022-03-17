using System.Linq;
using Hummingbird.DynamicRoute;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Hummingbird.Extensions.DynamicRoute.Consul.UnitTest
{
    [TestClass]
    public class ConsulServiceLocatorTest
    {
        [TestMethod]
        public async void when_tag_exists_success()
        {
            IServiceLocator serviceLocator = new ConsulServiceLocator("localhost", "8500", "dc1", "");
            var t = await serviceLocator.GetAsync("rfs-api", "dev");
            Assert.IsTrue(t.Count() > 0);
        }

        [TestMethod]
        public async void when_tag_notexists_success()
        {
            IServiceLocator serviceLocator = new ConsulServiceLocator("localhost", "8500", "dc1", "");
            var t = await serviceLocator.GetAsync("rfs-api", "ddd");
            Assert.IsTrue(t.Count() == 0);
        }
    }
}
