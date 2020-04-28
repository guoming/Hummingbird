using System;
using System.Collections.Generic;
using System.Text;

namespace Hummingbird.Extensions.UidGenerator.WorkIdCreateStrategy
{
    public interface IWorkIdCreateStrategy
    {
        int NextId();
    }
}
