using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Text;

namespace Hummingbird.Core
{
    public class ServiceConfig
    {
        public ServiceConfig()
        { }

        public string SERVICE_SELF_REGISTER { get; set; }
        public string SERVICE_NAME { get; set; }
        public string SERVICE_TAGS { get; set; }
        public string SERVICE_REGION { get; set; }

        public string SERVICE_REGISTRY_ADDRESS { get; set; }

        public string SERVICE_REGISTRY_PORT { get; set; }

        public string SERVICE_80_CHECK_HTTP { get; set; }

        public string SERVICE_80_CHECK_INTERVAL { get; set; }

        public string SERVICE_80_CHECK_TIMEOUT { get; set; }
        public string SERVICE_ADDRESS { get; set; }
        public string SERVICE_PORT { get; set; }
    }
}
