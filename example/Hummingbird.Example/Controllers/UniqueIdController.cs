
namespace Hummingbird.Example.Controllers
{
    using Hummingbird.Extensions.UidGenerator;
    using Microsoft.AspNetCore.Mvc;
    using System.Threading.Tasks;

    [Route("api/[controller]")]
    public class UniqueIdController : Controller
    {
        private readonly IUniqueIdGenerator uniqueIdGenerator;

        public UniqueIdController(
            IUniqueIdGenerator uniqueIdGenerator)
        {
            this.uniqueIdGenerator = uniqueIdGenerator;
        }

   

        [HttpGet]
        [Route("Test")]
        public async Task<long> Test()
        {
            return await Task.FromResult(uniqueIdGenerator.NewId());
        }
    }

}
