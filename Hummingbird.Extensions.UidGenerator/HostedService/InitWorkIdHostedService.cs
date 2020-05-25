using Hummingbird.Extensions.UidGenerator.WorkIdCreateStrategy;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Hummingbird.Extensions.UidGenerator
{
#if NETCORE

    public class InitWorkIdHostedService : Microsoft.Extensions.Hosting.IHostedService
    {
        private readonly IWorkIdCreateStrategy workIdCreateStrategy;

        public InitWorkIdHostedService(IWorkIdCreateStrategy  workIdCreateStrategy)
        {

            this.workIdCreateStrategy = workIdCreateStrategy;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            Console.WriteLine($"WorkId: {await workIdCreateStrategy.NextId()}");
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
#endif

}
