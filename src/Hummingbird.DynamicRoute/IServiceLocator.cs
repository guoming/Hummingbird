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

        Task<IEnumerable<ServiceEndPoint>> GetAsync(string Name, string TagFilter, CancellationToken cancellationToken = default(CancellationToken));

        Task<IEnumerable<ServiceEndPoint>> GetFromCacheAsync(string Name, string TagFilter, TimeSpan timeSpan,CancellationToken cancellationToken = default(CancellationToken));

    }
}
