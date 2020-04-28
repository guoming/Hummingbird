using System.Threading.Tasks;

namespace Hummingbird.Extensions.Resilience.Http
{
    public interface IHttpUrlResolver
    {
        Task<string> Resolve(string value);
    }
}