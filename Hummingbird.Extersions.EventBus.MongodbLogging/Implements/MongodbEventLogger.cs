using Dapper;
using Hummingbird.Extersions.EventBus.Abstractions;
using Hummingbird.Extersions.EventBus.Models;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading.Tasks;
using System.Linq;
using System.Threading;
using System.Data;
using Hummingbird.Extersions.UidGenerator;
using MongoDB.Driver;
using Microsoft.Extensions.Logging;

namespace Hummingbird.Extersions.EventBus.MongodbLogging
{
    public class MongodbConfiguration
    {
        public string ConnectionString { get; set; }

        public string DatabaseName { get; set; }

        /// <summary>
        /// 超时时间
        /// </summary>
        public int TimeoutMillseconds { get; set; } = 1000 * 20;
    }

    public class MongodbEventLogger : IEventLogger
    {
        IUniqueIdGenerator _uniqueIdGenerator;
        private readonly IMongoClient _client;
        private readonly MongodbConfiguration _mondbConfiguration;
        private ILogger<IMongoClient> _logger;
        private Polly.IAsyncPolicy _timeoutPolicy;
        public MongodbEventLogger(
            IUniqueIdGenerator uniqueIdGenerator,
            ILogger<IMongoClient> logger,
            IMongoClient client,
            MongodbConfiguration mondbConfiguration)
        {
            this._uniqueIdGenerator = uniqueIdGenerator;
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _mondbConfiguration = mondbConfiguration ?? throw new ArgumentNullException(nameof(client));
            _timeoutPolicy = Polly.Policy.TimeoutAsync(mondbConfiguration.TimeoutMillseconds);
        }

        /// <summary>
        /// 保存事件
        /// 作者:郭明
        /// 日期：2017年11月15日
        /// </summary>
        /// <param name="events"></param>
        /// <param name="transaction"></param>
        /// <returns></returns>
        public async Task<List<EventLogEntry>> SaveEventAsync(List<object> events, IDbTransaction transaction)
        {
            try
            {
                return await _timeoutPolicy.ExecuteAsync(async (ctx) =>
                {
                    var db = _client.GetDatabase(_mondbConfiguration.DatabaseName);
                    var collections = db.GetCollection<EventBus.Models.EventLogEntry>("events");
                    var models = new List<WriteModel<EventBus.Models.EventLogEntry>>();
                    var LogEntrys = events.Select(@event => new EventLogEntry("", @event, Guid.NewGuid().ToString("N"), _uniqueIdGenerator.NewId())).ToList();

                    foreach (var item in LogEntrys)
                    {
                        models.Add(new InsertOneModel<EventBus.Models.EventLogEntry>(item));
                    }

                    await collections.BulkWriteAsync(models, new BulkWriteOptions() { },  CancellationToken.None);
                    return LogEntrys;
                }, CancellationToken.None);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                throw ex;
            }
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
                try
                {
                    await _timeoutPolicy.ExecuteAsync(async (ctx) =>
                    {
                        var db = _client.GetDatabase(_mondbConfiguration.DatabaseName);
                        var collections = db.GetCollection<EventBus.Models.EventLogEntry>("events");
                        
                        await collections.UpdateOneAsync(o => events.Contains(o.EventId), Builders<EventBus.Models.EventLogEntry>.Update
                            .Set(a => a.State, EventStateEnum.Published)
                            .Inc(a => a.TimesSent, 1)
                           );

                    }, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);
                    throw ex;
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
                try
                {
                    await _timeoutPolicy.ExecuteAsync(async (ctx) =>
                     {
                         var db = _client.GetDatabase(_mondbConfiguration.DatabaseName);
                         var collections = db.GetCollection<EventBus.Models.EventLogEntry>("events");
                         await collections.UpdateOneAsync(o => events.Contains(o.EventId), Builders<EventBus.Models.EventLogEntry>.Update
                             .Set(a => a.State, EventStateEnum.PublishedFailed)
                             .Inc(a => a.TimesSent, 1)
                            );

                     }, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);
                    throw ex;
                }
            }
        }


        /// <summary>
        /// 发送发送失败或没有发送的消息
        /// </summary>
        /// <param name="Take"></param>
        /// <returns></returns>
        public List<EventLogEntry> GetUnPublishedEventList(int Take)
        {
            try
            {
               return _timeoutPolicy.ExecuteAsync(async (ctx) =>
                {
                    var db = _client.GetDatabase(_mondbConfiguration.DatabaseName);
                    var collections = db.GetCollection<EventBus.Models.EventLogEntry>("events");

                    return await collections.Find(o => (o.State == EventStateEnum.NotPublished || o.State == EventStateEnum.PublishedFailed) && o.TimesSent <= 3).SortBy(a => a.EventId).Limit(Take).ToListAsync();

                }, CancellationToken.None).Result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                throw ex;
            }
        }
    }
}
