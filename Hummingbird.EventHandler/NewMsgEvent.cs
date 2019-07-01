using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Hummingbird.WebApi.Events
{
    public class NewMsgEvent
    {
        public int Value { get; set; }
    }


}

namespace ZT.TMS.DataExchange.Application.Events
{
    public class ChangeDataCaptureEvent
    {
        public string database { get; set; }

        public string table { get; set; }

        public string type { get; set; }

        public Dictionary<string, dynamic> data { get; set; }

        public Dictionary<string, dynamic> old { get; set; }

        public long ts { get; set; }
    }
}
