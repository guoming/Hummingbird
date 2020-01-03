using System;
using System.Net;
using System.Text.RegularExpressions;

namespace Hummingbird.Extersions.UidGenerator.WorkIdCreateStrategy
{
    public class HostNameWorkIdCreateStrategy : IWorkIdCreateStrategy
    {

        /**
         * 根据机器名最后的数字编号获取工作进程Id.如果线上机器命名有统一规范,建议使用此种方式.
         * 例如机器的HostName为:(公司名-部门名-服务名-环境名-编号),会截取HostName最后的编号01作为workerId.         
         **/
        public int NextId()
        {

            var hostName = Dns.GetHostName();

            // 计算workerId的方式：
            // 第一步hostName.replaceAll("\\d+$", "")，即去掉hostname后纯数字部分，例如JTCRTVDRA44去掉后就是JTCRTVDRA
            // 第二步hostName.replace(第一步的结果, "")，即将原hostname的非数字部分去掉，得到纯数字部分，就是workerId
            if (int.TryParse(hostName.Replace(Regex.Replace(hostName, "\\d+$", ""), ""), out var workId))
            {
                return workId;
            }
            else
            {
                throw new Exception($"Wrong hostname:{hostName}, hostname must be end with number!");
            }
        }
    }
}
