using Dapper;
using Hummingbird.Extersions.EventBus.Abstractions;
using Hummingbird.Extersions.EventBus.Models;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading.Tasks;
using System.Linq;

namespace Hummingbird.Extersions.EventBus.SqlServerLogging
{
    public class SqlServerEventLogger : IEventLogger
    {
        IDbConnectionFactory _dbConnection;

        public SqlServerEventLogger(IDbConnectionFactory dbConnection)
        {
            this._dbConnection = dbConnection;
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
            var LogEntrys = events.Select(@event => new EventLogEntry(@event)).ToList();

            await transaction.Connection.ExecuteAsync("insert into EventLogs(EventId,EventTypeName,State,TimesSent,CreationTime,Content) values(@EventId,@EventTypeName,@State,@TimesSent,@CreationTime,@Content)",
            LogEntrys.Select(eventLogEntry => new
            {
                EventId = eventLogEntry.EventId,
                EventTypeName = eventLogEntry.EventTypeName,
                State = eventLogEntry.State,
                TimesSent = 0,
                CreationTime = DateTime.Now,
                Content = eventLogEntry.Content
            }), transaction: transaction);

            return LogEntrys;
        }

        /// <summary>
        /// 事件发布成功
        /// </summary>
        /// <param name="events"></param>
        /// <returns></returns>
        public async Task MarkEventAsPublishedAsync(List<string> events)
        {
            using (var db = _dbConnection.GetDbConnection())
            {
                if (db.State != System.Data.ConnectionState.Open)
                {
                    db.Open();
                }
                using (var tran = db.BeginTransaction())
                {
                    await db.ExecuteAsync("update EventLogs set TimesSent=TimesSent+1,State=1 where EventId=@EventId", events.Select(EventId => new { EventId = EventId }).ToList(), transaction: tran);
                    tran.Commit();
                }
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
                using (var db = _dbConnection.GetDbConnection())
                {
                    if (db.State != System.Data.ConnectionState.Open)
                    {
                        db.Open();
                    }
                    using (var tran = db.BeginTransaction())
                    {
                        await db.ExecuteAsync("update EventLogs set TimesSent=TimesSent+1,State=2 where EventId=@EventId", events.Select(EventId => new { EventId = EventId }).ToList(), transaction: tran);

                        tran.Commit();
                    }
                }
            }

            return;
        }

        async Task<int> MarkEventConsumeAsync(string EventId, string QueueName, int State)
        {
            using (var db = _dbConnection.GetDbConnection())
            {
                if (db.State != System.Data.ConnectionState.Open)
                {
                    db.Open();
                }

                using (var tran = db.BeginTransaction())
                {
                    var times = await db.ExecuteScalarAsync<int?>("update EventConsumeLogs set TimesConsume=TimesConsume+1,State=@State where EventId=@EventId and QueueName=@QueueName; " +
                                    "select TimesConsume from EventConsumeLogs where EventId=@EventId and QueueName=@QueueName;", new
                                    {
                                        EventId = EventId,
                                        QueueName = QueueName,
                                        State = State
                                    }, transaction: tran);

                    if (!times.HasValue)
                    {

                        await db.ExecuteAsync("insert into EventConsumeLogs(EventConsumeLogId,EventId,QueueName,State,TimesConsume,CreationTime) values(@EventConsumeLogId,@EventId,@QueueName,@State,0,@CreationTime)", new
                        {
                            EventConsumeLogId = Guid.NewGuid().ToString("N"),
                            EventId = EventId,
                            QueueName = QueueName,
                            State = State,
                            CreationTime = DateTime.Now
                        }, transaction: tran);

                        times = 0;
                    }

                    tran.Commit();

                    return times.Value;
                }
            }


        }

        /// <summary>
        /// 标识事件消费成功
        /// </summary>
        /// <param name="eventId"></param>
        /// <param name="queueName"></param>
        /// <returns></returns>
        public async Task MarkEventConsumeAsRecivedAsync(string EventId, string QueueName)
        {
            await MarkEventConsumeAsync(EventId, QueueName, 1);
        }

        /// <summary>
        /// 标识事件消费成功
        /// 作者：郭明
        /// 日期：2017年11月15日
        /// </summary>
        /// <param name="eventId"></param>
        /// <param name="queueName"></param>
        /// <returns></returns>
        public async Task<int> MarkEventConsumeAsFailedAsync(string EventId, string QueueName)
        {
            return await MarkEventConsumeAsync(EventId, QueueName, 2);
        }

        /// <summary>
        /// 发送发送失败或没有发送的消息
        /// </summary>
        /// <param name="Take"></param>
        /// <returns></returns>
        public List<EventLogEntry> GetUnPublishedEventList(int Take)
        {
            using (var db = _dbConnection.GetDbConnection())
            {
                return db.Query<EventLogEntry>("select top " + Take + " * from EventLogs where (State=0 or State=2) and TimesSent<=3 order by CreationTime asc").AsList();
            }
        }
    }
}
