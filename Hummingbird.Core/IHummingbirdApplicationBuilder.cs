
#if NETCORE
using Microsoft.AspNetCore.Builder;
#endif

namespace Hummingbird.Core
{
#if NETCORE
    public interface IHummingbirdApplicationBuilder
    {
        IApplicationBuilder app { get; }
    }
#endif
}

