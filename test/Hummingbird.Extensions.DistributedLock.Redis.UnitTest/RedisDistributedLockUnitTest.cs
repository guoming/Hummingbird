using System;
using System.Xml;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Hummingbird.Extensions.DistributedLock.Redis.UnitTest
{
    public class ConsulDistributedLockUnitTest
    {
        

        [Fact]
        public void Test1()
        {

            var cache= Hummingbird.Extensions.Cacheing.CacheFactory.Build(option=> {

                option.WithReadServerList("127.0.0.1:6379");
                option.WithWriteServerList("127.0.0.1:6379");
                option.WithDb(0);

            });

            
            var consulDistributedLock = new RedisDistributedLock(cache, null,TimeSpan.FromSeconds(10));
            for (int i = 0; i <= 100; i++)
            {
                consulDistributedLock.Enter("test-lock1", "");
            }

            //consulDistributedLock.Exit("test-lock1","");
        }
    }
}