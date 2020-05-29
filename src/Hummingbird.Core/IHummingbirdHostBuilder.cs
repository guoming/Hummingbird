using Microsoft.Extensions.DependencyInjection;

namespace Hummingbird.Core
{
    public  interface IHummingbirdHostBuilder
    {
        IServiceCollection Services { get; }
    }
}

