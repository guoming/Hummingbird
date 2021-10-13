using Microsoft.Extensions.DependencyInjection;

namespace Hummingbird.Extensions.Tracing
{
    internal class HummingbirdQuartzBuilder : IHummingbirdQuartzBuilder
    {
        private IServiceCollection _services;

        public HummingbirdQuartzBuilder(IServiceCollection Services)
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

