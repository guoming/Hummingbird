using System;
using System.Threading.Tasks;
using Quartz;

namespace Hummingbird.Example.Jobs
{
    public class HelloJob : IJob
    {
        public async Task Execute(IJobExecutionContext context)
        {
            await Console.Out.WriteLineAsync("Greetings from HelloJob!");
        }
    }
}
