using System;
using System.Collections.Generic;
using Hummingbird.Extensions.UidGenerator.Implements;
using Xunit;

namespace Hummingbird.Extensions.UidGenerator.UnitTest
{
    public class MacAddressProcessIdWorkIdCreateStrategyUnitTest
    {
        
        /// <summary>
        /// Mac地址一样，CenterId一样
        /// </summary>
        [Fact]
        public void when_macaddress_equal()
        {
            var processIdWorkIdCreateStrategy1 = new ProcessIdWorkIdCreateStrategy("00-00-00-00-00-00", 1);
            var processIdWorkIdCreateStrategy2 = new ProcessIdWorkIdCreateStrategy("00-00-00-00-00-00", 2);
            
            Assert.True(processIdWorkIdCreateStrategy1.GetCenterId()== processIdWorkIdCreateStrategy2.GetCenterId());
        }
        

        /// <summary>
        /// 创建32个workId
        /// </summary>
        [Fact]
        public void when_macaddress_create_32_centerId()
        {
            
            List<int> list = new List<int>();

            for (int i = 0; i < 32; i++)
            {
             
                var processIdWorkIdCreateStrategy1 = new ProcessIdWorkIdCreateStrategy($"00-00-00-00-00-{new Random().Next().ToString().PadLeft(2,'0')}", 0);

                var nextId = processIdWorkIdCreateStrategy1.GetCenterId();
                
                Assert.True(!list.Contains(nextId));
                
                if (!list.Contains(nextId))
                    list.Add(nextId);
            }
            
            

            Assert.True(list.Count== 32);
        }
       
        
        /// <summary>
        /// 一个机器上创建32个进程
        /// </summary>
        [Fact]
        public void when_macaddress_create_32_workid()
        {
            
            List<int> list = new List<int>();

            for (int i = 0; i < 32; i++)
            {
             
                var processIdWorkIdCreateStrategy1 = new ProcessIdWorkIdCreateStrategy("00-00-00-00-00-00", i);

                var nextId = processIdWorkIdCreateStrategy1.GetWorkId().Result;
                
                Assert.True(!list.Contains(nextId));
                
                if (!list.Contains(nextId))
                    list.Add(nextId);
            }
            
            

            Assert.True(list.Count== 32);
        }
        
       
     
    }
}