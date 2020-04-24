using Microsoft.Extensions.DependencyInjection;

namespace Hummingbird.Extensions.Tracing
{
    internal class HummingbirdOpenTracingBuilder : IHummingbirdOpenTracingBuilder
    {
        private IServiceCollection _services;

        public HummingbirdOpenTracingBuilder(IServiceCollection Services)
        {

            this._services = Services;
        }

        public IServiceCollection Services
        {
            get
            {
                return _services;
            }
        }
    }
}

