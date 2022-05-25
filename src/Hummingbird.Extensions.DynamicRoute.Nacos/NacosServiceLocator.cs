using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Hummingbird.DynamicRoute;
using Microsoft.Extensions.Caching.Memory;
using Nacos.V2;

namespace Hummingbird.Extensions.DynamicRoute.Nacos
{
    public class NacosServiceLocator : IServiceLocator
    {
        private readonly INacosNamingService  _nacosNamingService;
        private readonly MemoryCache _memoryCache;

        public NacosServiceLocator(INacosNamingService nacosNamingService)
        {
            _nacosNamingService = nacosNamingService;
            _memoryCache = new MemoryCache(new MemoryCacheOptions());
            this.Datacenter = "";
        }

        public string Datacenter { get; }

        public async Task<IEnumerable<ServiceEndPoint>> GetAsync(string Name, string TagFilter,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            var list = new List<ServiceEndPoint>();
            var allInstances = await _nacosNamingService.GetAllInstances(Name);
            var tagFilterList = TagFilter.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries).ToList();

            foreach (var instance in allInstances)
            {
                if (instance.Healthy)
                {
                    var tags = new List<String>();
                        
                    foreach (var tag in instance.Metadata)
                    {
                        tags.Add($"{tag.Key}={tag.Value}");
                    }
                    
                    if (tagFilterList.Any())
                    {
                        if (tags.Intersect(tagFilterList).Any())
                        {
                            list.Add(new ServiceEndPoint()
                            {
                                Address =instance.Ip,
                                Port = instance.Port,
                                Tags = tags.ToArray(),
                                Datacenter=instance.ClusterName,
                            });
                        }
                    }
                    else
                    {
                        list.Add(new ServiceEndPoint()
                        {
                            Address =instance.Ip,
                            Port = instance.Port,
                            Tags = tags.ToArray(),
                            Datacenter=instance.ClusterName,
                        });
                        
                    }
                }
            }

            return list;
        }

        public async Task<IEnumerable<ServiceEndPoint>> GetFromCacheAsync(string Name, string TagFilter, TimeSpan timeSpan,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            var cacheKey = $"{Name}:{TagFilter}";
            var cacheObj = _memoryCache.Get(cacheKey) as IEnumerable<ServiceEndPoint>;
            if(cacheObj==null)
            {
                cacheObj= await GetAsync(Name, TagFilter);

                _memoryCache.Set(cacheKey, cacheObj, timeSpan);
            }

            return await Task.FromResult(cacheObj);        }
    }
}