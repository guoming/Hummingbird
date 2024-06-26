
using Hummingbird.Example.DTO;

namespace Hummingbird.Example.Controllers
{
    using Hummingbird.Extensions.UidGenerator;
    using Microsoft.AspNetCore.Mvc;
    using System.Threading.Tasks;

    [Route("api/[controller]")]
    public class UniqueIdController : BaseController
    {
        private readonly IUniqueIdGenerator uniqueIdGenerator;

        public UniqueIdController(
            IUniqueIdGenerator uniqueIdGenerator)
        {
            this.uniqueIdGenerator = uniqueIdGenerator;
        }

   

        [HttpGet]
        [Route("Test")]
        public async Task<IApiResponse> Test()
        {
            return OK<long>( uniqueIdGenerator.NewId());
        }
    }

}
