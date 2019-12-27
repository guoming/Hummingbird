using Consul;
using Hummingbird.DynamicRoute;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Hummingbird.Extensions.DynamicRoute.Consul
{

    public class ConsulServiceLocator:IServiceLocator
    {
        private readonly ConsulClient _client;
        private readonly MemoryCache _memoryCache;

        public ConsulServiceLocator(
            string SERVICE_REGISTRY_ADDRESS, string SERVICE_REGISTRY_PORT, string SERVICE_REGION, string SERVICE_REGISTRY_TOKEN)
        {
            _client = new ConsulClient(delegate (ConsulClientConfiguration obj)
            {
                obj.Address = new Uri("http://" + SERVICE_REGISTRY_ADDRESS + ":" + SERVICE_REGISTRY_PORT);
                obj.Datacenter = SERVICE_REGION;
                obj.Token = SERVICE_REGISTRY_TOKEN;
            });
            _memoryCache = new MemoryCache(new MemoryCacheOptions());
        }

        public async Task<IEnumerable<ServiceEndPoint>> GetAsync(string Name,string TagFilter, CancellationToken cancellationToken = default(CancellationToken))
        {
            var list = new List<ServiceEndPoint>();
            var response = await _client.Agent.Services();
            var services = response.Response;
            var TagFilterList = TagFilter.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries).ToList();

            foreach (var p in services)
            {
                if (p.Value.Service.ToUpper() == Name.ToUpper())
                {
                    if (TagFilterList.Any())
                    {
                        if (p.Value.Tags.Intersect(TagFilterList).Any())
                        {
                            list.Add(new ServiceEndPoint()
                            {
                                Address = p.Value.Address,
                                Port = p.Value.Port,
                                Tags = p.Value.Tags,
                            });
                        }
                    }
                    else
                    {
                        list.Add(new ServiceEndPoint()
                        {
                            Address = p.Value.Address,
                            Port = p.Value.Port,
                            Tags = p.Value.Tags,
                        });
                    }
                }
            }

            return list;


        }

        public async Task<IEnumerable<ServiceEndPoint>> GetFromCacheAsync(string Name, string TagFilter,TimeSpan timeSpan, CancellationToken cancellationToken = default(CancellationToken))
        {
            var cacheKey = $"{Name}:{TagFilter}";
            var cacheObj = _memoryCache.Get(cacheKey) as IEnumerable<ServiceEndPoint>;
            if(cacheObj==null)
            {
                cacheObj= await GetAsync(Name, TagFilter);

                _memoryCache.Set(cacheKey, cacheObj, timeSpan);
            }

            return await Task.FromResult(cacheObj);

        }
    }
}
