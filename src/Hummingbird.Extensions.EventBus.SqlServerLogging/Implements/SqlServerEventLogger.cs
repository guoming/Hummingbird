using Dapper;
using Hummingbird.Extensions.EventBus.Abstractions;
using Hummingbird.Extensions.EventBus.Models;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading.Tasks;
using System.Linq;
using System.Threading;
using System.Data;
using Hummingbird.Extensions.UidGenerator;

namespace Hummingbird.Extensions.EventBus.SqlServerLogging
{
    public class SqlServerEventLogger : IEventLogger
    {
        private readonly IDbConnectionFactory _dbConnection;
        private readonly IUniqueIdGenerator _uniqueIdGenerator;
        private readonly SqlServerConfiguration _sqlServerConfiguration;
        

        public SqlServerEventLogger(
            IUniqueIdGenerator uniqueIdGenerator,
            IDbConnectionFactory dbConnection,
            SqlServerConfiguration sqlServerConfiguration)
        {
            this._sqlServerConfiguration = sqlServerConfiguration;
            this._uniqueIdGenerator = uniqueIdGenerator;
            this._dbConnection = dbConnection;
        }

        /// <summary>
        /// 事件发布成功
        /// </summary>
        /// <param name="events"></param>
        /// <returns></returns>
        public async Task MarkEventAsPublishedAsync(List<long> events, CancellationToken cancellationToken)
        {
            if (events != null)
            {
                var sqlParamtersList = new List<DynamicParameters>();
                foreach (var eventId in events)
                {
                    var sqlParamters = new DynamicParameters();
                    sqlParamters.Add("EventId", eventId, System.Data.DbType.Int64, System.Data.ParameterDirection.Input, 6);
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
                        await db.ExecuteAsync($"delete {_sqlServerConfiguration.TablePrefix}EventLogs where EventId=@EventId", sqlParamtersList, transaction: tran);
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
        public async Task MarkEventAsPublishedFailedAsync(List<long> events, CancellationToken cancellationToken)
        {
            if (events != null)
            {
                var sqlParamtersList = new List<DynamicParameters>();
                foreach (var eventId in events)
                {
                    var sqlParamters = new DynamicParameters();
                    sqlParamters.Add("EventId", eventId, System.Data.DbType.Int64, System.Data.ParameterDirection.Input, 6);
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
                        await db.ExecuteAsync($"update {_sqlServerConfiguration.TablePrefix}EventLogs set TimesSent=TimesSent+1,State=2 where EventId=@EventId", sqlParamtersList, transaction: tran);

                        tran.Commit();
                    }
                }
            }

            return;
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
                return db.Query<EventLogEntry>($"select top {Take} EventId,MessageId,TraceId,EventTypeName,State,TimesSent,CreationTime,Content from {_sqlServerConfiguration.TablePrefix}EventLogs where (State=0 or State=2) and TimesSent<=3 order by EventId asc").AsList();
            }
        }

        public async Task<List<EventLogEntry>> SaveEventAsync(List<EventLogEntry> LogEntrys, IDbTransaction transaction)
        {
            if (transaction == null)
            {
                throw new ArgumentNullException("transaction", $"A {typeof(DbTransaction).FullName} is required as a pre-requisite to save the event.");
            }
            var sqlParamtersList = new List<DynamicParameters>();
            foreach (var eventLogEntry in LogEntrys)
            {
                var sqlParamters = new DynamicParameters();
                sqlParamters.Add("EventId", eventLogEntry.EventId > 0 ? eventLogEntry.EventId : _uniqueIdGenerator.NewId(), System.Data.DbType.Int64, System.Data.ParameterDirection.Input, 6);
                sqlParamters.Add("MessageId", eventLogEntry.MessageId, System.Data.DbType.StringFixedLength, System.Data.ParameterDirection.Input, 50);
                sqlParamters.Add("TraceId", eventLogEntry.TraceId, System.Data.DbType.StringFixedLength, System.Data.ParameterDirection.Input, 50);
                sqlParamters.Add("EventTypeName", eventLogEntry.EventTypeName, System.Data.DbType.StringFixedLength, System.Data.ParameterDirection.Input, 500);
                sqlParamters.Add("State", eventLogEntry.State, System.Data.DbType.Int32, System.Data.ParameterDirection.Input, 4);
                sqlParamters.Add("TimesSent", 0, System.Data.DbType.Int32, System.Data.ParameterDirection.Input, 4);
                sqlParamters.Add("CreationTime", DateTime.UtcNow, System.Data.DbType.DateTime2, System.Data.ParameterDirection.Input, 4);
                sqlParamters.Add("Content", eventLogEntry.Content, System.Data.DbType.StringFixedLength, System.Data.ParameterDirection.Input);
                sqlParamtersList.Add(sqlParamters);
            }

            await transaction.Connection.ExecuteAsync($"insert into {_sqlServerConfiguration.TablePrefix}EventLogs(EventId,MessageId,TraceId,EventTypeName,State,TimesSent,CreationTime,Content) values(@EventId,@MessageId,@TraceId,@EventTypeName,@State,@TimesSent,@CreationTime,@Content)",
            sqlParamtersList,
            transaction: transaction
            );

            return LogEntrys;
        }
    }
}
