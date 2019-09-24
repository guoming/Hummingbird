#if NETCORE
using Microsoft.AspNetCore.Builder;

namespace Hummingbird.Core
{
    public class HummingbirdApplicationBuilder : IHummingbirdApplicationBuilder
    {
        private readonly IApplicationBuilder _app;

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

#endif