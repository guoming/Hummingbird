using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace Hummingbird.Core
{
    public class HummingbirdHostBuilder: IHummingbirdHostBuilder
    {
        private IServiceCollection _services;

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

