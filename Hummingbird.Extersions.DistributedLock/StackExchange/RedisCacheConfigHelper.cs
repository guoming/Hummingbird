using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Hummingbird.Extersions.DistributedLock.StackExchangeImplement
{ 
    static class RedisCacheConfigHelper
    {
        /// <summary>
        /// 分割服务器列表
        /// </summary>
        /// <param name="strSource"></param>
        /// <param name="split"></param>
        /// <returns></returns>
        public static IEnumerable<string> SplitString(string strSource, string split)
        {
            return strSource.Split(split.ToArray());
        }

        public static List<string> GetServerList(string value, string clusterName)
        {
            var array = SplitString(value, ",").ToList();
            List<string> result = new List<string>();
            for (int i = 0; i < array.Count; i++)
            {
                var schme = SplitString(array[i], "@").ToList();

                if (schme[0] == clusterName)
                {
                    result.Add(schme[1]);
                }
            }

            return result;
        }

        public static string GetServerClusterName(string value)
        {
            var list = SplitString(value, "@").ToList();
            if (list.Count == 2)
            {
                return list[0];
            }
            else
            {
                return "";
            }
        }

        public static string GetServerHost(string value)
        {
            var list = SplitString(value, "@").ToList();
            if (list.Count == 2)
            {
                return list[1];
            }
            else
            {
                return value;
            }
        }


        public static string GetIP(string IPAndPort)
        {
            var endPoint = RedisCacheConfigHelper.SplitString(IPAndPort, ":").ToList();
            var ip = endPoint[0]; //IP
            var port = int.Parse(endPoint[1]); //端口 

            return ip;
        }

        public static int GetPort(string IPAndPort)
        {
            var endPoint = RedisCacheConfigHelper.SplitString(IPAndPort, ":").ToList();
            var ip = endPoint[0]; //IP
            var port = int.Parse(endPoint[1]); //端口 

            return port;
        }
    }
}
