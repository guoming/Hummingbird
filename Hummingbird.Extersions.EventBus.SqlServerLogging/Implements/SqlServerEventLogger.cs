using Dapper;
using Hummingbird.Extersions.EventBus.Abstractions;
using Hummingbird.Extersions.EventBus.Models;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading.Tasks;
using System.Linq;
using System.Threading;

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

            var sqlParamtersList = new List<DynamicParameters>();
            foreach(var eventLogEntry in LogEntrys)
            {
                var sqlParamters = new DynamicParameters();
                sqlParamters.Add("EventId", eventLogEntry.EventId, System.Data.DbType.StringFixedLength, System.Data.ParameterDirection.Input, 32);
                sqlParamters.Add("EventTypeName", eventLogEntry.EventTypeName, System.Data.DbType.StringFixedLength, System.Data.ParameterDirection.Input, 500);
                sqlParamters.Add("State", eventLogEntry.State, System.Data.DbType.Int32, System.Data.ParameterDirection.Input, 4);
                sqlParamters.Add("TimesSent", 0, System.Data.DbType.Int32, System.Data.ParameterDirection.Input, 4);
                sqlParamters.Add("CreationTime", DateTime.Now, System.Data.DbType.DateTimeOffset, System.Data.ParameterDirection.Input, 4);
                sqlParamters.Add("Content", eventLogEntry.Content, System.Data.DbType.StringFixedLength, System.Data.ParameterDirection.Input, 500);
                sqlParamtersList.Add(sqlParamters);
            }

            await transaction.Connection.ExecuteAsync("insert into EventLogs(EventId,EventTypeName,State,TimesSent,CreationTime,Content) values(@EventId,@EventTypeName,@State,@TimesSent,@CreationTime,@Content)",
            sqlParamtersList, 
            transaction: transaction
            );

            return LogEntrys;
        }

        /// <summary>
        /// 事件发布成功
        /// </summary>
        /// <param name="events"></param>
        /// <returns></returns>
        public async Task MarkEventAsPublishedAsync(List<string> events, CancellationToken cancellationToken)
        {
            if (events != null)
            {
                var sqlParamtersList = new List<DynamicParameters>();
                foreach (var eventId in events)
                {
                    var sqlParamters = new DynamicParameters();
                    sqlParamters.Add("EventId", eventId, System.Data.DbType.StringFixedLength, System.Data.ParameterDirection.Input, 32);
                    sqlParamtersList.Add(sqlParamters);
                }

                using (var db = _dbConnection.GetDbConnection())
                {
                    if (db.State != System.Data.ConnectionState.Open)
                    {
                        await db.OpenAsync(cancellationToken);
                    }
                    using (var tran = db.BeginTransaction())
                    {
                        await db.ExecuteAsync("update EventLogs set TimesSent=TimesSent+1,State=1 where EventId=@EventId", sqlParamtersList, transaction: tran);
                        tran.Commit();
                    }
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
        public async Task MarkEventAsPublishedFailedAsync(List<string> events, CancellationToken cancellationToken)
        {
            if (events != null)
            {
                var sqlParamtersList = new List<DynamicParameters>();
                foreach (var eventId in events)
                {
                    var sqlParamters = new DynamicParameters();
                    sqlParamters.Add("EventId", eventId, System.Data.DbType.StringFixedLength, System.Data.ParameterDirection.Input, 32);
                    sqlParamtersList.Add(sqlParamters);
                }

                using (var db = _dbConnection.GetDbConnection())
                {
                    if (db.State != System.Data.ConnectionState.Open)
                    {
                        await db.OpenAsync(cancellationToken);
                    }
                    using (var tran = db.BeginTransaction())
                    {
                        await db.ExecuteAsync("update EventLogs set TimesSent=TimesSent+1,State=2 where EventId=@EventId", sqlParamtersList, transaction: tran);

                        tran.Commit();
                    }
                }
            }

            return;
        }

        async Task<int> MarkEventConsumeAsync(string[] EventIds, string QueueName, int State, CancellationToken cancellationToken)
        {
            var sqlQueryParamtersList = new List<DynamicParameters>();
            for(int i=0;i< EventIds.Length; i++)
            {
                var sqlParamters = new DynamicParameters();
                sqlParamters.Add("EventId", EventIds[i], System.Data.DbType.StringFixedLength, System.Data.ParameterDirection.Input, 32);
                sqlParamters.Add("QueueName", QueueName, System.Data.DbType.StringFixedLength, System.Data.ParameterDirection.Input, 32);
                sqlParamters.Add("State", State, System.Data.DbType.Int32, System.Data.ParameterDirection.Input, 4);
                sqlQueryParamtersList.Add(sqlParamters);
            }

            var sqlInsertLogParamtersList = new List<DynamicParameters>();
            for (int i = 0; i < EventIds.Length; i++)
            {
                var sqlParamters = new DynamicParameters();
                sqlParamters.Add("EventConsumeLogId", Guid.NewGuid().ToString("N"), System.Data.DbType.StringFixedLength, System.Data.ParameterDirection.Input, 32);
                sqlParamters.Add("EventId", EventIds[i], System.Data.DbType.StringFixedLength, System.Data.ParameterDirection.Input, 32);
                sqlParamters.Add("QueueName", QueueName, System.Data.DbType.StringFixedLength, System.Data.ParameterDirection.Input, 32);
                sqlParamters.Add("State", State, System.Data.DbType.Int32, System.Data.ParameterDirection.Input, 4);
                sqlParamters.Add("CreationTime", DateTime.Now, System.Data.DbType.DateTimeOffset, System.Data.ParameterDirection.Input, 4);
                sqlInsertLogParamtersList.Add(sqlParamters);
            }


            using (var db = _dbConnection.GetDbConnection())
            {
                if (db.State != System.Data.ConnectionState.Open)
                {
                    await db.OpenAsync(cancellationToken);
                }

                using (var tran = db.BeginTransaction())
                {
                    var times = await db.ExecuteScalarAsync<int?>("update EventConsumeLogs set TimesConsume=TimesConsume+1,State=@State where EventId=@EventId and QueueName=@QueueName; " +
                                                                "select TimesConsume from EventConsumeLogs where EventId=@EventId and QueueName=@QueueName;",
                                    sqlQueryParamtersList, transaction: tran);

                    if (!times.HasValue)
                    {
                        await db.ExecuteAsync("insert into EventConsumeLogs(EventConsumeLogId,EventId,QueueName,State,TimesConsume,CreationTime) values(@EventConsumeLogId,@EventId,@QueueName,@State,0,@CreationTime)", sqlInsertLogParamtersList, transaction: tran);
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
        public async Task MarkEventConsumeAsRecivedAsync(string[] EventIds, string QueueName, CancellationToken cancellationToken)
        {
            await MarkEventConsumeAsync(EventIds, QueueName, 1,cancellationToken);
        }

        /// <summary>
        /// 标识事件消费成功
        /// 作者：郭明
        /// 日期：2017年11月15日
        /// </summary>
        /// <param name="eventId"></param>
        /// <param name="queueName"></param>
        /// <returns></returns>
        public async Task<int> MarkEventConsumeAsFailedAsync(string[] EventIds, string QueueName, CancellationToken cancellationToken)
        {
            return await MarkEventConsumeAsync(EventIds, QueueName, 2,cancellationToken);
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
