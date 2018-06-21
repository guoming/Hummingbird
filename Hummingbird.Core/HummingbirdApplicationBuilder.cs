using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace Hummingbird.Core
{
    public class HummingbirdApplicationBuilder : IHummingbirdApplicationBuilder
    {
        private IApplicationBuilder _app;

        public HummingbirdApplicationBuilder(IApplicationBuilder app)
        {

            this._app = app;
        }

        public IApplicationBuilder app
        {
            get
            {
                return _app;
            }
        }
    }
}

