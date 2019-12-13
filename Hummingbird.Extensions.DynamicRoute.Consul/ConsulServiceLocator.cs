using Consul;
using Hummingbird.DynamicRoute;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
namespace Hummingbird.Extensions.DynamicRoute.Consul
{

    public class ConsulServiceLocator:IServiceLocator
    {
        private readonly ConsulClient _client;

        public ConsulServiceLocator(string SERVICE_REGISTRY_ADDRESS, string SERVICE_REGISTRY_PORT, string SERVICE_REGION, string SERVICE_REGISTRY_TOKEN)
        {
            _client = new ConsulClient(delegate (ConsulClientConfiguration obj)
            {
                obj.Address = new Uri("http://" + SERVICE_REGISTRY_ADDRESS + ":" + SERVICE_REGISTRY_PORT);
                obj.Datacenter = SERVICE_REGION;
                obj.Token = SERVICE_REGISTRY_TOKEN;
            });
        }

        public async Task<IEnumerable<ServiceEndPoint>> GetAsync(string Name, CancellationToken cancellationToken = default(CancellationToken))
        {
            var list = new List<ServiceEndPoint>();
            var response = await _client.Agent.Services();
            var services = response.Response;

            foreach(var p in services)
            {
                list.Add(new ServiceEndPoint()
                {
                    Address = p.Value.Address,
                    Port = p.Value.Port,
                    Tags = p.Value.Tags,
                });
            }

            return list;

        }
    }
}
