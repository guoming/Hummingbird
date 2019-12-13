using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Hummingbird.DynamicRoute
{
    public class ServiceEndPoint
    {
        public string Address { get; set; }

        public int Port { get; set; }

        public string[] Tags { get; set; }

    }

    public interface IServiceLocator
    {
        /// <summary>
        /// 获取一个服务代理
        /// </summary>
        Task<IEnumerable<ServiceEndPoint>> GetAsync(string Name, CancellationToken cancellationToken = default(CancellationToken));

    }
}
