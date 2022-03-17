using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Hummingbird.Example.Events.CanalEvent
{
    public class CanalEntryEvent
    {
        public dynamic data { get; set; }

        public string database { get; set; }

        public long es { get; set; }

        public long id { get; set; }

        public bool isDdl { get; set; }

        public dynamic mysqlType { get; set; }

        public dynamic old { get; set; }

        public dynamic pkNames { get; set; }


        public string sql { get; set; }

        public dynamic sqlType { get; set; }

        public string table { get; set; }

        public long ts { get; set; }
        public string type { get; set; }

    }
}
