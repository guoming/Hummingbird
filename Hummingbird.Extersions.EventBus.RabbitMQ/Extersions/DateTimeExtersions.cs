using System;
using System.Collections.Generic;
using System.Text;

namespace Hummingbird.Extersions.EventBus.RabbitMQ
{
    public static class DateTimeExtersions
    {
        public static long ToTimestamp(this DateTimeOffset nowTime)
        {
            return (nowTime.ToUniversalTime().Ticks - 621355968000000000) / 10000000;
        }


        public static long ToTimestamp(this DateTime nowTime)
        {
            return (nowTime.ToUniversalTime().Ticks - 621355968000000000) / 10000000;
        }

        /// <summary>        
        /// 时间戳转为C#格式时间        
        /// </summary>        
        /// <param name=”timeStamp”></param>        
        /// <returns></returns>        
        public static DateTime ToUtcDateTime(this long unixTimeStamp)
        {
            System.DateTime startTime = new System.DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            DateTime dt = startTime.AddSeconds(unixTimeStamp);
            return dt;
        }
    }
}
