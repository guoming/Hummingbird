using Hummingbird.EventBus;
using System.Threading.Tasks;

namespace Hummingbird.EventBus.Abstractions
{
    /// <summary>
    /// 事件处理程序
    /// 作者：郭明
    /// 日期：2017年11月15日
    /// </summary>
    /// <typeparam name="TEvent"></typeparam>
    public interface IEventHandler<in TEvent> 
        where TEvent: EventEntity
    {
        Task<bool> Handle(TEvent @event);
    }
}
