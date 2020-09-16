using System;
using System.Collections.Generic;
using System.Text;

namespace Hummingbird.Extensions.Canal
{
    public interface IConnector
    {
        bool Process(List<Com.Alibaba.Otter.Canal.Protocol.Entry> entrys, IFormater formater);
    }
}
