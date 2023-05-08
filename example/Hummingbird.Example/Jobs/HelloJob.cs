using System;
using System.Threading.Tasks;
using Hummingbird.Extensions.DistributedLock;
using Quartz;

namespace Hummingbird.Example.Jobs
{
    public class HelloJob : IJob
    {
        private readonly IDistributedLock _distributedLock;

        public HelloJob(
            Hummingbird.Extensions.DistributedLock.IDistributedLock distributedLock)
        {
            _distributedLock = distributedLock;
        }
        
        public async Task Execute(IJobExecutionContext context)
        {
            var lockResult = _distributedLock.Enter("HelloJob","");
            if (!lockResult.Acquired)
            {
                return;
            }

            await Console.Out.WriteLineAsync("Greetings from HelloJob!");

            _distributedLock.Exit(lockResult);
        }
    }
}
