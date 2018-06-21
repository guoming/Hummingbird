using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Text;

namespace Hummingbird.Extersions.ServiceRegistry
{
    public class ServiceConfig
    {
        public ServiceConfig()
        { }

        /// <summary>
        /// 服务注册中心地址
        /// </summary>
        public string SERVICE_REGISTRY_ADDRESS { get; set; }
        /// <summary>
        /// 服务注册中心端口
        /// </summary>
        public string SERVICE_REGISTRY_PORT { get; set; }
        /// <summary>
        /// 服务注册中心访问Token
        /// </summary>
        public string SERVICE_REGISTRY_TOKEN { get; set; }
        /// <summary>
        /// 采用服务自注册模式
        /// </summary>
        public string SERVICE_SELF_REGISTER { get; set; }

        /// <summary>
        /// 主机名
        /// </summary>
        public string HOSTNAME { get; set; }
        /// <summary>
        /// 服务名称（服务发现名称）
        /// </summary>
        public string SERVICE_NAME { get; set; }
        /// <summary>
        /// 服务标签
        /// </summary>
        public string SERVICE_TAGS { get; set; }
        /// <summary>
        /// 服务区域（隔离）
        /// </summary>
        public string SERVICE_REGION { get; set; }
        /// <summary>
        /// 服务访问地址（DNS或IP）
        /// </summary>
        public string SERVICE_ADDRESS { get; set; }
        /// <summary>
        /// 服务端口号
        /// </summary>
        public string SERVICE_PORT { get; set; }

        /// <summary>
        /// Http健康检查地址
        /// </summary>
        public string SERVICE_80_CHECK_HTTP { get; set; }
        /// <summary>
        /// 服务监控检查周期
        /// </summary>
        public string SERVICE_80_CHECK_INTERVAL { get; set; }
        /// <summary>
        /// 服务监控检查超时时间
        /// </summary>
        public string SERVICE_80_CHECK_TIMEOUT { get; set; }
    }
}
