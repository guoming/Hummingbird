using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace Hummingbird.Extensions.RequestLimit
{
    internal class HummingbirdRequestLimitBuilder: IHummingbirdRequestLimitBuilder
    {
        private IServiceCollection _services;

        public HummingbirdRequestLimitBuilder(IServiceCollection Services)
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

