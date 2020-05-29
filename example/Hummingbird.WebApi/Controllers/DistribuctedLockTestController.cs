
namespace Hummingbird.Example.Controllers
{
    using Microsoft.AspNetCore.Mvc;
    using System;
    using System.Threading.Tasks;

    [Route("api/[controller]")]
    public class DistributedLockController : Controller
    {
        public DistributedLockController(Hummingbird.Extensions.DistributedLock.IDistributedLock distributedLock)
        {
            this.distributedLock = distributedLock;
        }

      
        private readonly Hummingbird.Extensions.DistributedLock.IDistributedLock distributedLock;

        [HttpGet]
        [Route("Test")]
        public async Task<string> Test()
        {
            var lockName = "name";
            var lockToken = Guid.NewGuid().ToString("N");
            try
            {
                if (distributedLock.Enter(lockName, lockToken, TimeSpan.FromSeconds(30)))
                {
                    // do something
                    return "ok";
                }
                else
                {
                    return "error";
                }
            }
            finally
            {
                distributedLock.Exit(lockName, lockToken);
            }
            
        }

    }

}
