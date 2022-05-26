
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
            try
            {
                if (distributedLock.Enter(lockName, lockToken, TimeSpan.FromSeconds(60), retryAttemptMillseconds:1000,retryTimes:10))
                {
                   await  System.Threading.Tasks.Task.Delay(5000);

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
