using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace Hummingbird.Extensions.Configuration.Json
{
    public class JsonConfigurationSource : Microsoft.Extensions.Configuration.Json.JsonConfigurationSource
    {
        public override IConfigurationProvider Build(IConfigurationBuilder builder)
        {
            return new JsonConfigurationProvider(this);
        }
    }
}
