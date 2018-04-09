using Hummingbird.EventBus.Abstractions;
using Hummingbird.EventBus.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;

namespace Hummingbird.EventBus.EntityframeworkSqlServer
{
    public class EventLogService : IEventLogService
    {
        private readonly EventLogContext _eventLogContext;

        public EventLogService(EventLogContext dbContext)
        {
            this._eventLogContext = dbContext;
        }

        public EventLogService(string connectionString)
        {
            var _connectionString = connectionString ?? throw new ArgumentNullException("connectionString");

            _eventLogContext = new EventLogContext(
                new DbContextOptionsBuilder<EventLogContext>()
                    .UseSqlServer(_connectionString)
                    .ConfigureWarnings(warnings => warnings.Throw(RelationalEventId.QueryClientEvaluationWarning))
                    .Options);
        }

        /// <summary>
        /// 保存事件
        /// 作者:郭明
        /// 日期：2017年11月15日
        /// </summary>
        /// <param name="events"></param>
        /// <param name="transaction"></param>
        /// <returns></returns>
        public async Task<List<EventLogEntry>> SaveEventAsync(List<object> events, DbTransaction transaction)
        {
            if (transaction == null)
            {
                throw new ArgumentNullException("transaction", $"A {typeof(DbTransaction).FullName} is required as a pre-requisite to save the event.");
            }

            var _eventLogContext = new EventLogContext(
             new DbContextOptionsBuilder<EventLogContext>()
                 .UseSqlServer(transaction.Connection)
                 .ConfigureWarnings(warnings => warnings.Throw(RelationalEventId.QueryClientEvaluationWarning))
                 .Options);

            var LogEntrys = new List<EventLogEntry>();
            foreach (var @event in events)
            {
                var eventLogEntry = new EventLogEntry(@event);
                LogEntrys.Add(eventLogEntry);
            }

            _eventLogContext.Database.UseTransaction(transaction);
            _eventLogContext.EventLogs.AddRange(LogEntrys);
            await _eventLogContext.SaveChangesAsync();
            return LogEntrys;
        }

        /// <summary>
        /// 事件发布成功
        /// </summary>
        /// <param name="events"></param>
        /// <returns></returns>
        public async Task MarkEventAsPublishedAsync(List<string> events)
        {
            if (events != null)
            {
                foreach (var EventId in events)
                {
                    var eventLogEntry = await _eventLogContext.EventLogs.Where(ie => ie.EventId == EventId).FirstOrDefaultAsync();
                    if (eventLogEntry != null)
                    {
                        eventLogEntry.TimesSent++;
                        eventLogEntry.State = EventStateEnum.Published;
                        _eventLogContext.EventLogs.Update(eventLogEntry);
                    }
                }

                await _eventLogContext.SaveChangesAsync();
            }
        }

        /// <summary>
        /// 事件发布失败
        /// 作者：郭明
        /// 日期：2017年11月15日
        /// </summary>
        /// <param name="events"></param>
        /// <returns></returns>
        public async Task MarkEventAsPublishedFailedAsync(List<string> events)
        {
            if (events != null)
            {

                foreach (var EventId in events)
                {
                    var eventLogEntry = await _eventLogContext.EventLogs.Where(ie => ie.EventId == EventId).FirstOrDefaultAsync();
                    if (eventLogEntry != null)
                    {
                        eventLogEntry.TimesSent++;
                        eventLogEntry.State = EventStateEnum.PublishedFailed;
                        _eventLogContext.EventLogs.Update(eventLogEntry);
                    }
                }

                await _eventLogContext.SaveChangesAsync();
            }
        }

        /// <summary>
        /// 标识事件消费成功
        /// </summary>
        /// <param name="eventId"></param>
        /// <param name="queueName"></param>
        /// <returns></returns>
        public async Task MarkEventConsumeAsRecivedAsync(string EventId, string queueName)
        {
            var eventModel = await _eventLogContext.EventConsumeLogs.Where(a => a.EventId == EventId && queueName == a.QueueName).FirstOrDefaultAsync();

            if (eventModel != null)
            {
                eventModel.TimesConsume++;
                eventModel.State = EventConsumeStateEnum.Reviced;
                _eventLogContext.EventConsumeLogs.Update(eventModel);
            }
            else
            {
                eventModel = new EventConsumeLogEntry()
                {
                    CreationTime = DateTime.Now,
                    EventId = EventId,
                    TimesConsume = 1,
                    State = EventConsumeStateEnum.Reviced,
                    QueueName = queueName
                };
                _eventLogContext.EventConsumeLogs.Add(eventModel);
            }

            await _eventLogContext.SaveChangesAsync();

            return;
        }

        /// <summary>
        /// 标识事件消费成功
        /// 作者：郭明
        /// 日期：2017年11月15日
        /// </summary>
        /// <param name="eventId"></param>
        /// <param name="queueName"></param>
        /// <returns></returns>
        public async Task<int> MarkEventConsumeAsFailedAsync(string EventId, string queueName)
        {
            var eventModel = await _eventLogContext.EventConsumeLogs.Where(a => a.EventId == EventId && queueName == a.QueueName).FirstOrDefaultAsync();

            if (eventModel != null)
            {
                eventModel.TimesConsume++;
                eventModel.State = EventConsumeStateEnum.RevicedFailed;
                _eventLogContext.EventConsumeLogs.Update(eventModel);
            }
            else
            {
                eventModel = new EventConsumeLogEntry()
                {
                    CreationTime = DateTime.Now,
                    EventId = EventId,
                    TimesConsume = 0,
                    State = EventConsumeStateEnum.RevicedFailed,
                    QueueName = queueName
                };
                _eventLogContext.EventConsumeLogs.Add(eventModel);
            }

            await _eventLogContext.SaveChangesAsync();

            return eventModel.TimesConsume;
        }

        /// <summary>
        /// 发送发送失败或没有发送的消息
        /// </summary>
        /// <param name="Take"></param>
        /// <returns></returns>
        public List<EventLogEntry> GetUnPublishedEventList(int Take)
        {
            //查询没有发送的消息
            var eventEntryList = _eventLogContext.EventLogs.Where(a => (a.State == EventStateEnum.NotPublished || a.State == EventStateEnum.PublishedFailed) && a.TimesSent <= 3).Take(Take).OrderBy(a => a.CreationTime).ToList();

            return eventEntryList;
        }
    }
}
