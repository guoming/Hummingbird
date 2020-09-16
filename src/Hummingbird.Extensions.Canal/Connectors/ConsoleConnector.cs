using Com.Alibaba.Otter.Canal.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Hummingbird.Extensions.Canal.Connectors
{
    public class ConsoleConnector : IConnector
    {
      
        public bool Process(List<Entry> entrys,IFormater jsonFormater)
        {
            foreach (var entry in entrys.Where(entry => entry.EntryType == Com.Alibaba.Otter.Canal.Protocol.EntryType.Rowdata))
            {
                Console.WriteLine(jsonFormater.Format(entry));
            }

            return true;

        }
    }
}
