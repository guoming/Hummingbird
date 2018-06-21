using Hummingbird.Extersions.EventBus.Models;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading.Tasks;

namespace Hummingbird.Extersions.EventBus.Abstractions
{
    /// <summary>
    /// 事件日志
    /// 作者：郭明
    /// 日期：2017年11月15日
    /// </summary>
    public interface IEventLogger
    {
         Task<List<EventLogEntry>> SaveEventAsync(List<object> events, DbTransaction transaction);

        /// <summary>
        /// 事件已经发布成功
        /// 作者：郭明
        /// 日期：2017年11月15日
        /// </summary>
        /// <param name="events"></param>
        /// <returns></returns>
        Task MarkEventAsPublishedAsync(List<string> events);

        /// <summary>
        /// 事件发布失败
        /// 作者：郭明
        /// 日期：2017年11月15日
        /// </summary>
        /// <param name="events"></param>
        /// <returns></returns>
        Task MarkEventAsPublishedFailedAsync(List<string> events);

        /// <summary>
        /// 消费成功
        /// </summary>
        /// <param name="events"></param>
        /// <returns></returns>
        Task MarkEventConsumeAsRecivedAsync(string @event,string queueName);

        /// <summary>
        /// 消费失败
        /// </summary>
        /// <param name="events"></param>
        /// <returns></returns>
        Task<int> MarkEventConsumeAsFailedAsync(string @event, string queueName);


        //Dictionary<string,Dictionary<string,string>> GetUnPublishedEventList(int Take);

        List<EventLogEntry> GetUnPublishedEventList(int Take);

        
    }
}
