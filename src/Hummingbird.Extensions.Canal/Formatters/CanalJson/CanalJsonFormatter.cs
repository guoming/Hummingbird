using Com.Alibaba.Otter.Canal.Protocol;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Hummingbird.Extensions.Canal.Formatters.CanalJson
{
    public class Formatter : IFormater
    {
        public object Format(Com.Alibaba.Otter.Canal.Protocol.Entry entry)
        {
            return entry;
         
        }
    }
}
