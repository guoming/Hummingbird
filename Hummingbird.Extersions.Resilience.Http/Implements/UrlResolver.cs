using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Linq;
using System.Text.RegularExpressions;

namespace Hummingbird.Extersions.Resilience.Http
{
    public class HttpUrlResolver : IHttpUrlResolver
    {
        private readonly Hummingbird.DynamicRoute.IServiceLocator _serviceLocator;
        private readonly Hummingbird.LoadBalancers.DefaultLoadBalancerFactory<Hummingbird.DynamicRoute.ServiceEndPoint> _balancerFactory;
        private readonly Hummingbird.LoadBalancers.ILoadBalancer<Hummingbird.DynamicRoute.ServiceEndPoint> _loadBalancer;

        public HttpUrlResolver(            
            Hummingbird.DynamicRoute.IServiceLocator serviceLocator)
        {
            this._serviceLocator = serviceLocator;
            this._balancerFactory = new Hummingbird.LoadBalancers.DefaultLoadBalancerFactory<Hummingbird.DynamicRoute.ServiceEndPoint>();
            this._loadBalancer = _balancerFactory.Get(() => new List<Hummingbird.DynamicRoute.ServiceEndPoint>());
        }

        public async Task<string> Resolve(string value)
        {
            var result = value;
            var paramList = GetParameters(result);
            foreach (var param in paramList)
            {
                if (!string.IsNullOrEmpty(param))
                {
                    //获取服务地址
                    var endPoints = await _serviceLocator.GetAsync(param);
                    //获取一个地址
                    var targetEndPoint = _loadBalancer.Lease(endPoints.ToList());
                    
                    result = result.Replace("{" + param + "}", $"{targetEndPoint.Address}:{targetEndPoint.Port}");
                }
            }
            return result;
        }

        private List<string> GetParameters(string text)
        {
            var matchVale = new List<string>();
            string Reg = @"(?<=\{)[^\${}]*?(?=})";
            string key = string.Empty;
            foreach (Match m in Regex.Matches(text, Reg))
            {
                matchVale.Add(m.Value.TrimEnd('|'));
            }
            return matchVale;
        }
    }
}
