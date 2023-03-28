using System.Threading.Tasks;

namespace Hummingbird.Extensions.UidGenerator.Implements
{
    class StaticWorkIdCreateStrategy : IWorkIdCreateStrategy
    {
        private readonly int _WorkId;
        private readonly int _centerId;
        
    
        public StaticWorkIdCreateStrategy(int CenterId,int WorkId)
        {  
            _centerId = CenterId;
            _WorkId = WorkId;
        }
        
        public int GetCenterId()
        {
            return _centerId;
        }

        /// <summary>
        /// 获取1~32之间的数字
        /// </summary>
        /// <returns></returns>
        public Task<int> GetWorkId()
        {
            return Task.FromResult(_WorkId);
        }
    }
}
