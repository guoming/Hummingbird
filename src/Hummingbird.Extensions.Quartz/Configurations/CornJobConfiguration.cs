using System;
using Quartz;
namespace Hummingbird.Extensions.Quartz
{
    public class CornJobConfiguration
    {
        public bool Open { get; set; }

        public CronTrigger[] CronTriggers { get; set; }

        public class CronTrigger
        {
            public bool Open { get; set; }

            public string Name { get; set; }

            public string Group { get; set; }

            public string JobName { get; set; }

            public string JobType { get; set; }

            public string JobGroup { get; set; }

            public string Expression { get; set; }

            public JobDataMap Configuration { get; set; }

        }
    }
}
