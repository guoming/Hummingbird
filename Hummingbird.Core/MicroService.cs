using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Ocelot.DownstreamUrlCreator;
using Ocelot.ServiceDiscovery;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace HealthCloud.MicroServiceCore
{
   


    public class MicroService
    {
        IConfiguration configuration;
        IUrlBuilder urlBuilder;
        ServiceConfig _serviceConfig;

        string serviceName;


        public async Task<List<Ocelot.Values.Service>> GetEndpoints(string serviceName)
        {
            
            var config = new ConsulRegistryConfiguration(_serviceConfig.SERVICE_REGISTRY_ADDRESS, int.Parse(_serviceConfig.SERVICE_REGISTRY_PORT), serviceName);
            IServiceDiscoveryProvider discoveryProvider = new ConsulServiceDiscoveryProvider(config);
            var list = await discoveryProvider.Get();
            return list;
        }

        public MicroService(
            IConfiguration configuration,
            IUrlBuilder urlBuilder,
            IOptions<ServiceConfig> options,
            string serviceName)
        {   
            this.configuration = configuration;
            this.serviceName = serviceName;
            this.urlBuilder = urlBuilder;
            this. _serviceConfig = options.Value;
        }

        public async Task<string> BuildUrlAsync(string schme,string apiPath)
        {
        
                var serviceEndPoints = await GetEndpoints(serviceName);
                //负载均衡
                var _roundRobinLoadBlancer = new Ocelot.LoadBalancer.LoadBalancers.RoundRobin(() => Task.FromResult(serviceEndPoints));

                //服务主机名和端口
                var _servicehostAndPort = await _roundRobinLoadBlancer.Lease();

                if (!_servicehostAndPort.IsError)
                {
                    var downStreamUrl = urlBuilder.Build(apiPath, schme, _servicehostAndPort.Data);

                    if (!downStreamUrl.IsError)
                    {
                        return downStreamUrl.Data.Value;
                    }
                    else
                    {
                        throw new System.Net.Http.HttpRequestException("");
                    }
                }
                else
                {
                    throw new System.Net.Http.HttpRequestException("LoadBalancer");
                }
            
        }
    }
        


}
