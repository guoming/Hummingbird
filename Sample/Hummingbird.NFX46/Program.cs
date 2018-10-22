using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hummingbird.NFX46
{
    public class User
    {
        public string Name { get; set; }
    }

    class Program
    {
        static void Main(string[] args)
        {
            var distributedLock = Hummingbird.Extersions.DistributedLock.DistributedLockFactory.CreateRedisDistributedLock("", "", new Extersions.DistributedLock.RedisCacheConfig("", "", ""));
            

            Action<Extersions.Cache.RedisConfigurationBuilder> setting = (config) =>
            {

                config.WithAllowAdmin()
                .WithDatabase(0)
                .WithPassword("tY7cRu9HG_jyDw2r")
                .WithEndpoint("192.168.10.229", 63100);
            };


            var cacheManager = Hummingbird.Extersions.Cache.CacheFactory.Build<string>(setting);
            var _userCacheManager = Hummingbird.Extersions.Cache.CacheFactory.Build<User>(setting);
            _userCacheManager.Add("user1", new User() { Name = "123" }, TimeSpan.FromSeconds(2), "test");
            var suer = _userCacheManager.Get("user1", "test");
            Console.WriteLine(suer.Name);

            while (true)
            {
                var line = Console.ReadLine();
                 if(line=="D")
                {
                    cacheManager.Delete("TestMulitLevelCache", "Test");

                }
                else if (!string.IsNullOrEmpty(line))
                {
                    cacheManager.Add("TestMulitLevelCache", line, TimeSpan.FromSeconds(6), "Test");
                }
                else
                    line = cacheManager.Get("TestMulitLevelCache", "Test");

                Console.WriteLine("Output:" + line);
            }
            //Console.ReadKey();

        }
    }
}
