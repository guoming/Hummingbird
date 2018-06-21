using System;
using System.Collections.Generic;
using System.Text;

namespace Hummingbird.Extersions.UidGenerator.WorkIdCreateStrategy
{
    public interface IWorkIdCreateStrategy
    {
        int NextId();
    }
}
