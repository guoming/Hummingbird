using System.Threading.Tasks;

namespace Hummingbird.Extersions.Resilience.Http
{
    public interface IHttpUrlResolver
    {
        Task<string> Resolve(string value);
    }
}