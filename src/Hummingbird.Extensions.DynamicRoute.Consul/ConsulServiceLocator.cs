using Consul;
using Hummingbird.DynamicRoute;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Hummingbird.Extensions.DynamicRoute.Consul
{

    public class ConsulServiceLocator:IServiceLocator
    {
        private readonly ILogger<ConsulServiceLocator> _logger;
        private readonly ConsulClient _client;
        private readonly MemoryCache _memoryCache;
        private readonly string _dataCenter = "";
       
        public string Datacenter
        {
            get
            {
                return _dataCenter;
            }
        }

        public ConsulServiceLocator(
            ILogger<ConsulServiceLocator> logger,
            ConsulClient client)
        {
            _logger = logger;
            _client = client;
            _memoryCache = new MemoryCache(new MemoryCacheOptions());
            _dataCenter = client.Config.Datacenter;
        }

        public async Task<IEnumerable<ServiceEndPoint>> GetAsync(string Name, string TagFilter, CancellationToken cancellationToken = default(CancellationToken))
        {
         
            //获取所有数据中心列表
            var centers = await GetDataCenters(cancellationToken);

            //查询当前数据中心的
            var allList = await GetAsync(Name, TagFilter, _dataCenter, cancellationToken);

            //获取其他数据中心服务列表
            foreach (var center in centers)
            {
                if(center!=_dataCenter)
                {
                    try
                    {
                        var list=  await GetAsync(Name, TagFilter, center, cancellationToken);

                        allList= allList.Union(list);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex.Message,ex);
                    }
                }
            }

            return allList;
        }


        
        /// <summary>
        /// 获取所有数据中心列表
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private  async Task<String[]> GetDataCenters(CancellationToken cancellationToken=default(CancellationToken))
        {
            var response = await _client.Catalog.Datacenters(cancellationToken);
            var centers = new List<string>();
            return response.Response.ToArray();
        }

        private async Task<IEnumerable<ServiceEndPoint>> GetAsync(string Name,string TagFilter, string Datacenter, CancellationToken cancellationToken = default(CancellationToken))
        {
            var list = new List<ServiceEndPoint>();
            var response = await _client.Health.Service(Name,string.Empty,false,new QueryOptions() {  Datacenter= Datacenter}, cancellationToken);
            var services = response.Response;
            var TagFilterList = TagFilter.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries).ToList();
 
            foreach (var p in services)
            {  
                if (p.Service.Service.ToUpper() == Name.ToUpper())
                {
                    if (p.Checks.All(a => a.Status.Status == HealthStatus.Passing.Status))
                    {
                        if (TagFilterList.Any())
                        {
                            if (p.Service.Tags.Intersect(TagFilterList).Any())
                            {
                                list.Add(new ServiceEndPoint()
                                {
                                    Address = p.Service.Address,
                                    Port = p.Service.Port,
                                    Tags = p.Service.Tags,
                                    Datacenter=Datacenter,
                                });
                            }
                        }
                        else
                        {
                            list.Add(new ServiceEndPoint()
                            {
                                Address = p.Service.Address,
                                Port = p.Service.Port,
                                Tags = p.Service.Tags,
                                Datacenter=Datacenter
                            });
                        }
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
