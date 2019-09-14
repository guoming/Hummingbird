using Microsoft.Extensions.Configuration.Json;
using System;
using System.IO;

namespace Hummingbird.Extensions.Configuration.Json
{
    public class JsonConfigurationProvider : Microsoft.Extensions.Configuration.Json.JsonConfigurationProvider
    {
        JsonConfigurationParser parse = new JsonConfigurationParser();

        public JsonConfigurationProvider(
            JsonConfigurationSource source):base(source)
        {
        
        }

        public override void Load(Stream stream)
        {
            Data = this.parse.Parse(stream,null);

        }
    }
}
