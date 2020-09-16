using System;
using System.Collections.Generic;
using System.Text;

namespace Hummingbird.Extensions.Canal
{
    public interface IFormater
    {
        object Format(Com.Alibaba.Otter.Canal.Protocol.Entry entry);
    }
}
