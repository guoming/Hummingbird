using System.Threading.Tasks;

namespace Hummingbird.Extensions.UidGenerator.Implements
{
    class StaticWorkIdCreateStrategy : IWorkIdCreateStrategy
    {
        private readonly int _WorkId;

        public StaticWorkIdCreateStrategy(int WorkId)
        {
            _WorkId = WorkId;

        }
        
        /// <summary>
        /// 获取1~32之间的数字
        /// </summary>
        /// <returns></returns>
        public Task<int> NextId()
        {
            return Task.FromResult(_WorkId);
        }
    }
}
