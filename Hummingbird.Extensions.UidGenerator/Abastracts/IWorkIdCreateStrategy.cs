using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Hummingbird.Extensions.UidGenerator.WorkIdCreateStrategy
{
    public interface IWorkIdCreateStrategy
    {
        Task<int> NextId();
    }
}
