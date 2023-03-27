using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Hummingbird.Extensions.UidGenerator
{
    public interface IWorkIdCreateStrategy
    {
        int GetCenterId();
        Task<int> GetWorkId();
    }
}
