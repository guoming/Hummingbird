using System;
using System.Collections.Generic;
using System.Text;

namespace Hummingbird.Extensions.OpenTracking.Jaeger
{
    public class TracingConfiguration
    {
        public bool Open { get; set; } = false;

        /// <summary>
        /// 刷新周期
        /// </summary>
        public int FlushIntervalSeconds { get; set; } = 15;

        public string SerivceName { get; set; } = "Example";

        /// <summary>
        /// 采样类型（默认：全量）
        /// </summary>
        public string SamplerType { get; set; } = "const";

        /// <summary>
        /// 记录日志
        /// </summary>
        public bool LogSpans { get; set; } = true;

        public string EndPoint { get; set; }

        public int AgentPort { get; set; } = 5575;

        public string AgentHost { get; set; } = "localhost";
    }
}
