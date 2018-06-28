using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace Hummingbird.Extersions.ServiceRegistry
{
    public class ServiceConfig
    {

        public static string GetIpAddress()
        {
            String hostName = Dns.GetHostName();
            IPHostEntry ipH = Dns.GetHostEntry(hostName);
            if (ipH.AddressList.Length >= 1)
            {
                return ipH.AddressList[0].ToString();
            }
            else
            {
                return hostName;
            }
        }

        public ServiceConfig()
        { }

        /// <summary>
        /// 服务注册中心地址（默认:consul）
        /// </summary>
        public string SERVICE_REGISTRY_ADDRESS { get; set; } = "consul";
        /// <summary>
        /// 服务注册中心端口(默认：8500)
        /// </summary>
        public string SERVICE_REGISTRY_PORT { get; set; } = "8500";
        /// <summary>
        /// 服务注册中心访问Token
        /// </summary>
        public string SERVICE_REGISTRY_TOKEN { get; set; } = "";
        /// <summary>
        /// 采用服务自注册模式(默认:false)
        /// </summary>
        public string SERVICE_SELF_REGISTER { get; set; } = "false";

        /// <summary>
        /// 服务唯一ID（ServiceName:HostName:Port）
        /// </summary>
        public string SERVICE_ID
        {
            get
            {
               return $"{SERVICE_NAME}:{ SERVICE_ADDRESS}:{SERVICE_PORT}";
            }
        }

        /// <summary>
        /// 服务名称（服务发现名称）
        /// </summary>
        public string SERVICE_NAME { get; set; }
        /// <summary>
        /// 服务标签
        /// </summary>
        public string SERVICE_TAGS { get; set; } = "";

        /// <summary>
        /// 服务区域（隔离）
        /// </summary>
        public string SERVICE_REGION { get; set; } = "";

        string _SERVICE_ADDRESS;
        /// <summary>
        /// 服务访问地址（DNS或IP）
        /// </summary>
        ///<value>默认自动获取</value>
        public string SERVICE_ADDRESS
        {

            get
            {
                if (string.IsNullOrEmpty(_SERVICE_ADDRESS))
                {
                   _SERVICE_ADDRESS= GetIpAddress();
                }

                return _SERVICE_ADDRESS;
            }
            set
            {

                _SERVICE_ADDRESS = value;
            }
        }
        /// <summary>
        /// 服务端口号(默认:80)
        /// </summary>
        public string SERVICE_PORT { get; set; } = "80";

        /// <summary>
        /// Http健康检查地址(默认:/healthcheck)
        /// </summary>
        public string SERVICE_80_CHECK_HTTP { get; set; } = "/healthcheck";
        /// <summary>
        /// 服务监控检查周期(默认:15s)
        /// </summary>
        public string SERVICE_80_CHECK_INTERVAL { get; set; } = "15s";
        /// <summary>
        /// 服务监控检查超时时间(默认:5s)
        /// </summary>
        public string SERVICE_80_CHECK_TIMEOUT { get; set; } = "5s";
    }
}
