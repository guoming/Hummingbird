using System;
using System.Collections.Generic;
using System.Text;

namespace Hummingbird.Extensions.Canal
{
    public interface ISubscripter
    {
        bool Process(CanalEventEntry[] entrys);
    }
}
