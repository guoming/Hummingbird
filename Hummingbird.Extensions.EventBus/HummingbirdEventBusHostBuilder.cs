using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace Hummingbird.Extensions.EventBus
{
    internal class HummingbirdEventBusHostBuilder: IHummingbirdEventBusHostBuilder
    {
        private IServiceCollection _services;

        public HummingbirdEventBusHostBuilder(IServiceCollection Services)
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

