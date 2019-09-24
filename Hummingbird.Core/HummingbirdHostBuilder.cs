using Microsoft.Extensions.DependencyInjection;

namespace Hummingbird.Core
{
    public class HummingbirdHostBuilder: IHummingbirdHostBuilder
    {
        private readonly IServiceCollection _services;

        public HummingbirdHostBuilder(IServiceCollection Services)
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

