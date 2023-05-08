
using Hummingbird.Extensions.DistributedLock;

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
        [Route("Test/{lockName}")]
        public async Task<string> Test(string lockName="key1")
        {
            var lockToken = Guid.NewGuid().ToString("N");
            
            using (LockResult lockResult = distributedLock.Enter(
                       lockName,
                       lockToken))
            {
                try
                {
                    if (lockResult.Acquired)
                    {
                        await System.Threading.Tasks.Task.Delay(15000);

                        // do something
                        return "ok";
                    }
                    else
                    {
                        return "error";
                    }
                }
                catch (Exception ex)
                {
                    return ex.Message;
                }
                finally
                {
                    distributedLock.Exit(lockResult);
                }
            }

        }

    }

}
