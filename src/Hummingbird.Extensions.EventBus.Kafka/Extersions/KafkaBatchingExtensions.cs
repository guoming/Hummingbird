﻿using Confluent.Kafka;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Hummingbird.Extensions.EventBus.Abstractions;
using Microsoft.Extensions.Logging;

namespace Hummingbird.Extensions.EventBus.Kafka.Extersions
{
    public static class KafkaBatchingExtensions
    {
        // It is strongly recommended to only use this with consumers configured with `enable.auto.offset.store=false`
        // since some of the consumes in the batch may succeed prior to encountering an exception, without the caller
        // ever having seen the messages.
        public static IEnumerable<ConsumeResult<TKey, TVal>> ConsumeBatch<TKey, TVal>(this IConsumer<TKey, TVal> consumer,
            TimeSpan maxWaitTime, int maxBatchSize, CancellationToken cts = default(CancellationToken))
        {
            var waitBudgetRemaining = maxWaitTime;
            var deadline = DateTime.UtcNow + waitBudgetRemaining;
            var res = new List<ConsumeResult<TKey, TVal>>();
            var resSize = 0;

            while (waitBudgetRemaining > TimeSpan.Zero && DateTime.UtcNow < deadline && resSize < maxBatchSize)
            {
                cts.ThrowIfCancellationRequested();
              
                var msg = consumer.Consume(waitBudgetRemaining);

                if (msg != null && !msg.IsPartitionEOF)
                {
                    res.Add(msg);
                    resSize++;
                }

                waitBudgetRemaining = deadline - DateTime.UtcNow;
            }

            return res;
        }

        public static async Task ProduceBatchAsync<TKey, TVal>(
            this IProducer<TKey, TVal> producer,
            ILogger<IEventBus> logger,
            string topic,
            IEnumerable<Message<TKey, TVal>> messages,
            TimeSpan flushTimeout,
            TimeSpan flushWait,
            CancellationToken cts = default(CancellationToken))
        {
            
            //错误报告
            var errorReports = new ConcurrentQueue<DeliveryResult<TKey, TVal>>();
            //期望接收数量
            var reportsExpected = 0;
            //实际接收的数量
            var reportsReceived = 0;
            
            var tasks = new List<Task<DeliveryResult<TKey,TVal>>>();
            
            foreach (var message in messages)
            {
                int partation = GetPartation(message.Headers);
                tasks.Add(producer.ProduceAsync(new TopicPartition(topic, new Partition(partation)), message, cts));
                reportsExpected++;
            }

            var allTaskCompleted= await Task.WhenAll(tasks).ContinueWith(async state =>
            {
                foreach (var deliveryResult in state.Result)
                {
                    //消息没有被持久化，则写入异常报告中
                    if (deliveryResult.Status == PersistenceStatus.Persisted)
                    {
                        Interlocked.Increment(ref reportsReceived);
                    }
                }

                await Task.FromResult(reportsExpected);

            },TaskContinuationOptions.OnlyOnRanToCompletion);

            await allTaskCompleted;
            
            //如果实际接收数量小于期望数量，则抛出异常
            if (reportsReceived < reportsExpected)
            {
                var msg =
                    $"Kafka producer flush did not complete within the timeout; only received {reportsReceived} " +
                    $"delivery reports out of {reportsExpected} expected.";
                throw new Exception(msg);
            }
        }
        
        static int GetPartation(Headers headers)
        {
            int partation = 0;

            int.TryParse(System.Text.Encoding.UTF8.GetString(headers.GetLastBytes("x-partition")), out partation);
            return partation;

        }
    }

   
}
